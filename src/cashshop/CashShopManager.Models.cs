
using Logger;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CashShopManager
    {
        protected List<UserCashShopItem> ToUserCashShopItems(List<DatabaseResultItemSet>? rows)
        {
            List<UserCashShopItem> items = new List<UserCashShopItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    UserCashShopItem? item = v.To<UserCashShopItem>();
                    if (item == null) { continue; }

                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBGetUserCashItems(string user_uid,
                            List<UserCashShopItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {

                //
                List<DatabaseResultItemSet>? list = null;

                // 
                string sql =
                $"SELECT " +
                $"	  cs.`uid`, cs.`id`, cs.`product_id`, cs.`name`, cs.`type`, " +
                $"    cs.`user_id`, cs.`custom_id`, u.`name` AS `user_name`, " +
                $"    cs.`count`, cs.`amount`, cs.`balance`, " +
                $"    cs.`item_0`, cs.`item_1`, cs.`item_2`, " +
                $"    cs.`create_time`, cs.`custom_data`, " +
                $"    cs.`status` " +
                $"FROM `t_cashshop_items` AS cs  " +
                $"LEFT JOIN `t_user` AS u ON u.`id` = cs.`user_id` " +
                $"WHERE  " +
                $"    u.`status` > 0 AND cs.`status` > 0 AND" +
                $"    u.`id` = ? ";
                var result_code = db?.QueryWithList(sql, out list,
                    user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

                items.AddRange(this.ToUserCashShopItems(list));

                return items.Count;
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
    }
}