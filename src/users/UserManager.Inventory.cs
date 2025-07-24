

using AMToolkits.Extensions;
using Logger;

namespace Server
{
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
        public DateTime? create_time = null;
        public DateTime? expired_time = null;
        public DateTime? remaining_time = null;
        public DateTime? using_time = null;
        public int status = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        /// <summary>
        /// 
        /// </summary>
        public async Task UpdateUserInventoryItems(string? user_uid, List<AMToolkits.Game.GeneralItemData>? items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return;
            }

            if (items == null)
            {
                return;
            }

            if (await DBUpdateUserInventoryItems(user_uid, items) < 0)
            {
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) (User:{user_uid}) Failed");
                return;
            }
        }
    }
}