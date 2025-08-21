
using Logger;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGameEventData
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("id")]
        public int ID = -1;
        [JsonPropertyName("name")]
        public string Name = "";

        [JsonPropertyName("user_id")]
        public string UserID = "";
        [JsonPropertyName("type")]
        public int EventType = 0;
        [JsonPropertyName("count")]
        public int Count = 0;

        [JsonPropertyName("items")]
        public string Items = "";
    }

    [System.Serializable]
    public class GameEventDataResult
    {
        public int Code = -1;
        public int ID = -1;
        public List<AMToolkits.Game.GeneralItemData>? Items = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GameEventsManager : AMToolkits.SingletonT<GameEventsManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static GameEventsManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public GameEventsManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[GameEventsManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.MapPost("api/game/events/final", HandleGameEventFinal);

        }


        /// <summary>
        ///  完成游戏活动或事件
        /// </summary>
        /// <param name="user_uid">玩家ID</param>
        /// <param name="id">流水单号</param>
        /// <param name="index">索引</param>
        public async System.Threading.Tasks.Task<Server.GameEventDataResult> GameEventFinal(string user_uid, int id)
        {
            var result = new GameEventDataResult()
            {
                Code = -1,
                ID = id,
            };

            // 必须
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return result;
            }
            // 
            var template_item = template_data.First(v => v.Id == id);
            if (template_item == null)
            {
                return result;
            }

            //
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null)
            {
                result.Code = -2; //未验证
                return result;
            }

            var event_data = new NGameEventData()
            {
                ID = id,
                Name = template_item.Name,
                UserID = r_user.ID,
                EventType = template_item.EventType,
                Count = 1,
            };

            // 检测时间
            double t_start = (template_item.StartDateTime - DateTime.Now).TotalSeconds;
            if (t_start > 0)
            {
                result.Code = -3;
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Date: {template_item.StartDateTime} Not Start");
                return result;
            }
            double t_end = (template_item.EndDateTime - DateTime.Now).TotalSeconds;
            if (t_end <= 0)
            {
                result.Code = -3;
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Date: {template_item.EndDateTime} End");
                return result;
            }

            // 添加数据库记录
            int result_code = 0;
            if ((result_code = await UserManager.Instance._UpdateGameEventData(r_user.ID, r_user.CustomID, event_data)) < 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Failed");
                return result;
            }

            if (result_code == 0)
            {
                result.Code = 0;
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Repeat Final");
                return result;
            }

            // 1 : 结算道具
            // 获取道具 (是否有物品发放)
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(template_item.Items);
            if (items != null && items.Length > 0)
            {
                var item_list = UserManager.Instance.InitGeneralItemData(items);
                if (item_list == null)
                {
                    result.Code = 0;
                    _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Add Items Failed");
                    return result;
                }

                // 需要对齐
                int index = 1000;
                foreach (var v in item_list)
                {
                    v.NID = ++index;
                }

                // 发放物品 :
                result_code = await UserManager.Instance._AddUserInventoryItems(user_uid, item_list);
                if (result_code < 0)
                {
                    result.Code = 0;
                    _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Add Items Failed");
                    return result;
                }
                
                result.Items = new List<AMToolkits.Game.GeneralItemData>(item_list);

                // 添加数据库记录
                if (await UserManager.Instance._UpdateGameEventItemData(r_user.ID, r_user.CustomID, event_data, result.Items) <= 0)
                {
                    result.Code = 0;
                    _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Add Items Failed");
                    return result;
                }
            }

            //
            _logger?.Log($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Completed");
            result.Code = 1;
            return result;
        }

    }
}