

using AMToolkits.Extensions;
using Logger;

namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUserInventoryItem
    {
        public string iid = "";
        public int index = 0;
        public string name = "";
        public int count = 0;
        public DateTime? create_time = null;
        public DateTime? expired_time = null;
        public DateTime? remaining_time = null;
        public DateTime? using_time = null;
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserInventoryItem
    {
        public int uid = 0;
        public string iid = "";
        public string server_uid = ""; //t_user表中的
        public int index = 0;
        public string name = "";
        public int count = 0;
        public DateTime? create_time = null;
        public DateTime? expired_time = null;
        public DateTime? remaining_time = null;
        public DateTime? using_time = null;
        public int status = 0;

        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NUserInventoryItem ToNItem()
        {
            return new NUserInventoryItem()
            {
                iid = this.iid,
                index = this.index,
                name = this.name,
                count = this.count,
                create_time = this.create_time,
                expired_time = this.expired_time,
                remaining_time = this.remaining_time,
                using_time = this.using_time,
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
        /// 物品更新
        /// </summary>
        public async Task _UpdateUserInventoryItems(string? user_uid, List<AMToolkits.Game.GeneralItemData>? items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return;
            }

            if (items == null)
            {
                return;
            }

            if (await _DBUpdateUserInventoryItems(user_uid, items) < 0)
            {
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) (User:{user_uid}) Failed");
                return;
            }
        }
        #endregion

        #region Client General
        /// <summary>
        /// 获取物品列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> GetUserInventoryItems(string? user_uid, List<NUserInventoryItem> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if (await DBGetUserInventoryItems(user_uid, list) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserInventoryItems) (User:{user_uid}) Failed");
                return -1;
            }

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            // 转换为可通用的物品类
            foreach (var v in list)
            {
                var item_template_data = template_data?.Get(v.index);
                items.Add(v.ToNItem());
            }

            return items.Count;
        }

        public async Task<int> UsingUserInventoryItem(string? user_uid, string item_iid, int item_index,
                                    List<NUserInventoryItem> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var item_template_data = template_data?.Get(item_index);
            if (item_template_data == null)
            {
                return -1;
            }

            // 该物品不允许使用
            if (!item_template_data.Useable)
            {
                return -10;
            }

            int result_code = 0;
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if ((result_code = await DBUsingUserInventoryItem(user_uid, item_iid, item_template_data, list)) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserInventoryItems) (User:{user_uid}) Failed");
                return -1;
            }

            // 物品不存在
            if (result_code == 0)
            {
                return 0;
            }

            // 物品已经在使用
            if (result_code == 1)
            {
                return 1;
            }

            // 转换为可通用的物品类
            foreach (var v in list)
            {
                item_template_data = template_data?.Get(v.index);
                items.Add(v.ToNItem());
            }

            return result_code;

        }

        #endregion
    }
}