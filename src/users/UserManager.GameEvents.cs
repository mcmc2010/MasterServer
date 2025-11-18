

using AMToolkits.Extensions;
using Logger;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class GameEventItem
    {
        public int uid = 0;
        public int id = -1;
        public string server_uid = ""; //t_user表中的
        public string name = "";
        public string? value = null;
        public int count = 0;
        public int event_type = 0;
        public int event_sub_type = 0;
        public int group_index = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;
        public DateTime? completed_time = null;

        private List<AMToolkits.Game.GeneralItemData> _items = new List<AMToolkits.Game.GeneralItemData>();
        private AMToolkits.Utility.ITableData? _template_data = null;

        public int season = 0;
        public int status = 0;

        public List<AMToolkits.Game.GeneralItemData> Items => this._items;

        public bool IsCompleted
        {
            get { return completed_time != null; }
        }

        public void InitTemplateData<T>(T templete_data) where T : AMToolkits.Utility.ITableData
        {
            _template_data = templete_data;
        }

        public T? GetTemplateData<T>() where T : AMToolkits.Utility.ITableData
        {
            return (T?)this._template_data;
        }


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="items"></param>
        public void InitGeneralItems(IEnumerable<AMToolkits.Game.GeneralItemData>? items)
        {
            this._items.Clear();

            if (items != null)
            {
                this._items.AddRange(items);
            }
        }

        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NGameEventData ToNItem()
        {
            return new NGameEventData()
            {
                //UserID = this.server_uid,
                ID = this.id,
                Name = this.name,
                Count = this.count,
                EventType = this.event_type,
                EventSubType = this.event_sub_type,
                GroupIndex = this.group_index,
                CreateTime = this.create_time,
                LastTime = this.last_time,
                CompletedTime = this.completed_time,
                Items = "",
                Season = this.season,
            };
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <param name="type"></param>
        /// <param name="group_index"></param>
        /// <returns></returns>
        public async Task<int> _GetUserGameEvents(string user_uid,
                            List<GameEventItem> items,
                            int type = -1,
                            int group_index = -1)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            if (await DBGetGameEvents(user_uid, items, type, group_index) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserGameEvents) (User:{user_uid}) Failed");
                return -1;
            }

            return items.Count;
        }

        public async Task<int> _UpdateGameEventItem(string user_uid, GameEventItem? item)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if (item == null)
            {
                return -1;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBUpdateGameEventItem(user_uid, item)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="custom_uid"></param>
        /// <param name="item"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> _UpdateGameEventVirtualCurrency(string user_uid, string custom_uid,
                            GameEventItem? item, List<string> currency_list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (item == null)
            {
                return -1;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBUpdateGameEventVirtualCurrency(user_uid, item, currency_list)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 事件更新
        ///     - 仅仅更新物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="custom_uid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> _UpdateGameEventItemData(string user_uid, string custom_uid,
                            GameEventItem? item, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (item == null)
            {
                return -1;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBUpdateGameEventItemData(user_uid, item, items)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 事件更新
        ///   - 不包括物品
        /// </summary>
        public async Task<int> _UpdateGameEventData(string user_uid, string custom_uid,
                            NGameEventData? data)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (data == null)
            {
                return -1;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBUpdateGameEventData(user_uid, data)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        #endregion

        public async Task<int> GetUserGameEvents(string user_uid,
                            List<NGameEventData> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            List<GameEventItem> list = new List<GameEventItem>();
            if (await DBGetGameEvents(user_uid, list) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserGameEvents) (User:{user_uid}) Failed");
                return -1;
            }

            // 转换为可通用的类
            foreach (var v in list)
            {
                items.Add(v.ToNItem());
            }

            return items.Count;
        }
    }
}