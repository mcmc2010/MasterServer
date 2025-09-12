
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class GameEventsManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="template_item"></param>
        /// <param name="result">上一个调用结果</param>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_Normal(UserBase user,
                                    int id,
                                    Game.TGameEvents template_item,
                                    NGameEventData event_data,
                                    Server.GameEventDataResult result)
        {
            // 获取道具 (是否有物品发放)
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(template_item.Items);
            if (items == null || items.Length == 0)
            {
                return 0;
            }
            
            //
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                result.Code = 0;
                _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items Failed");
                return -1;
            }

            // 需要对齐
            int index = 1000;
            foreach (var v in item_list)
            {
                v.NID = ++index;
            }

            // 发放物品 :
            var result_code = await UserManager.Instance._AddUserInventoryItems(user.ID, item_list);
            if (result_code < 0)
            {
                result.Code = 0;
                _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items Failed");
                return -1;
            }

            result.Items = new List<AMToolkits.Game.GeneralItemData>(item_list);

            // 添加数据库记录
            if (await UserManager.Instance._UpdateGameEventItemData(user.ID, user.CustomID, event_data, result.Items) <= 0)
            {
                result.Code = 0;
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items Failed");
                return -1;
            }

            return 1;
            
        }
    }
}