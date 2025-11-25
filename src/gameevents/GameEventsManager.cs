
using Logger;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public enum GameEventType
    {
        None = 0,
        Normal = 1,     //普通事件
        Record = 4,     //记录性事件
        Economy = 7,
        Payment = 10,
        Rank = 100,     //排位段位
        Pass = 1000,    //通行证
    }

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
        [JsonPropertyName("sub_type")]
        public int EventSubType = 0;
        [JsonPropertyName("group_index")]
        public int GroupIndex = 0;

        [JsonPropertyName("count")]
        public int Count = 0;

        [JsonPropertyName("value")]
        public string Value = "";

        [JsonPropertyName("items")]
        public string Items = "";

        [JsonPropertyName("record")]
        public string Record = "";

        [JsonPropertyName("season")]
        public int Season = 0;

        [JsonPropertyName("create_time")]
        public DateTime? CreateTime = null;
        [JsonPropertyName("last_time")]
        public DateTime? LastTime = null;
        [JsonPropertyName("completed_time")]
        public DateTime? CompletedTime = null;
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

            //
            int result_code = await _GameEventFinal(user_uid, id, result);
            if (result_code <= 0)
            {
                result.Code = result_code;
                return result;
            }

            result.Code = result_code;
            return result;
        }
        

        public async System.Threading.Tasks.Task<int> _GameEventFinal(string user_uid, int id,
                        GameEventDataResult result)
        {
            result.ID = id;

            // 必须
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null || id <= 0)
            {
                return -1;
            }
            // 
            var template_item = template_data.First(v => v.Id == id);
            if (template_item == null)
            {
                return -1;
            }

            //
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null)
            {
                return -2;
            }

            var event_data = new NGameEventData()
            {
                ID = id,
                Name = template_item.Name,
                UserID = r_user.ID,
                EventType = template_item.EventType,
                Count = 1,

                //
                Season = GameSettingsInstance.Settings.Season.Code,
            };

            // 检测时间
            double t_start = (template_item.StartDateTime - DateTime.Now).TotalSeconds;
            if (t_start > 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Date: {template_item.StartDateTime} Not Start");
                return -3;
            }
            double t_end = (template_item.EndDateTime - DateTime.Now).TotalSeconds;
            if (t_end <= 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Date: {template_item.EndDateTime} End");
                return -3;
            }

            // 添加数据库记录
            int result_code = 0;
            if ((result_code = await UserManager.Instance._UpdateGameEventData(r_user.ID, r_user.CustomID, event_data)) < 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Failed");
                return result_code;
            }

            if (result_code == 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Repeat Final");
                return 0;
            }

            List<GameEventItem> result_events = new List<GameEventItem>();
            switch (template_item.EventType)
            {
                // 1  : 结算道具
                case (int)GameEventType.Normal:
                    {
                        result_code = await GameEventFinal_Normal(r_user, id, template_item, result_events);
                        if (result_code > 0)
                        {
                            result_code = await GameEventFinal_Result(r_user, id, template_item, result_events, result);
                        }
                        break;
                    }
                // 4 :
                case (int)GameEventType.Record:
                    {
                        break;
                    }
                // 10 : 付费或充值
                case (int)GameEventType.Payment:
                    {
                        result_code = await GameEventFinal_Payment(r_user, id, template_item, result_events);
                        if (result_code > 0)
                        {
                            result_code = await GameEventFinal_Result(r_user, id, template_item, result_events, result);
                        }
                        break;
                    }

                // 100 : 排位
                case (int)GameEventType.Rank:
                    {
                        result_code = await GameEventFinal_GameRank(r_user, id, template_item, result_events);
                        if (result_code > 0)
                        {
                            result_code = await GameEventFinal_Result(r_user, id, template_item, result_events, result);
                        }
                        break;    
                    }
                // 1000: 通行证
                case (int)GameEventType.Pass:
                    {
                        List<AMToolkits.Game.GeneralItemData> output_item_list = new List<AMToolkits.Game.GeneralItemData>();
                        result_code = await GameEventFinal_GamePass(r_user, id, template_item, result_events, output_item_list);
                        if (result_code > 0)
                        {
                            result_code = await GameEventFinal_Result(r_user, id, template_item, result_events, result, output_item_list);
                        }
                        break;
                    }
            }

            if (result_code <= 0)
            {
                return result_code;
            }

            
            //
            _logger?.Log($"{TAGName} (GameEventFinal) (User:{user_uid}) {id} - {template_item.Name} Completed");
            return 1;
        }

    }
}