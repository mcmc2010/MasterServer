
using System.Runtime.InteropServices;
using AMToolkits.Extensions;



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
        public int count = 0;
        public int event_type = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;
        public DateTime? completed_time = null;

        private List<AMToolkits.Game.GeneralItemData> _items = new List<AMToolkits.Game.GeneralItemData>();
        private AMToolkits.Utility.ITableData? _template_data = null;
        public int status = 0;

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
        public void InitGeneralItems(AMToolkits.Game.GeneralItemData[]? items)
        {
            this._items.Clear();

            if (items != null)
            {
                this._items.AddRange(items);
            }
        }


    }


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal
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

        /// <summary>
        /// 事件更新
        ///     - 仅仅更新物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="custom_uid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> _UpdateGameEventItemData(string user_uid, string custom_uid,
                            NGameEventData? data, List<AMToolkits.Game.GeneralItemData> items)
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
            if ((result_code = await _DBUpdateGameEventItemData(user_uid, data, items)) < 0)
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
    }
}