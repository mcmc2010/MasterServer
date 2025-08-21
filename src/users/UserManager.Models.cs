//// 新增设备纪录
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using AMToolkits.Game;

using Game;
using Logger;
using Mysqlx.Crud;


//
namespace Server
{
    [System.Serializable]
    public class DBUserProfile
    {
        [JsonPropertyName("nid")]
        public int NID = -1;

        [JsonPropertyName("uid")]
        public string UID = "";

        [JsonPropertyName("name")]
        public string Name = "";

        [JsonPropertyName("gender")]
        public int Gender = (int)UserGender.Female;

        [JsonPropertyName("region")]
        public string Region = "";

        /// <summary>
        /// 目前头像只能选择已有图像
        /// </summary>
        [JsonPropertyName("avatar_id")]
        public int AvatarID = 0;
        /// <summary>
        /// 未使用
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl = "";


    }


    [System.Serializable]
    public class DBUserProfileExtend 
    {
        [JsonPropertyName("nid")]
        public int NID = -1;

        [JsonPropertyName("uid")]
        public string UID = "";
        /// <summary>
        /// 当前赛季 段位
        /// </summary>
        [JsonPropertyName("rank_level")]
        public int RankLevel = 0;

        /// <summary>
        /// 当前赛季 段位
        /// </summary>
        [JsonPropertyName("rank_value")]
        public int RankValue = 0;

        /// <summary>
        /// 上赛季 段位
        /// </summary>
        [JsonPropertyName("last_rank_level")]
        public int LastRankLevel = 0;

        /// <summary>
        /// 上赛季 段位
        /// </summary>
        [JsonPropertyName("last_rank_value")]
        public int LastRankValue = 0;

        /// <summary>
        /// 大师印记
        /// </summary>
        [JsonPropertyName("challenger_reals")]
        public int ChallengerReals = 0;

        /// <summary>
        /// 玩家参与的赛季
        /// </summary>
        [JsonPropertyName("season")]
        public int Season = 1;

        /// <summary>
        /// 玩家参与的赛季时间
        /// </summary>
        [JsonPropertyName("season_time")]
        public DateTime? SeasonTime = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_data"></param>
        /// <returns></returns>
        protected int DBAuthUser(UserAuthenticationData user_data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                int privilege_level = 0;

                // PlayFab 
                string sql =
                    $"SELECT uid, id AS server_id, client_id, token, passphrase, last_time, privilege_level, status " +
                    $"FROM t_user WHERE client_id = ? AND playfab_id = ? AND status >= 0;";
                var result_code = db?.Query(sql, user_data.client_uid, user_data.custom_id);
                if (result_code < 0)
                {
                    return -1;
                }
                // 不存在
                // 自动创建新纪录
                else if (result_code == 0)
                {
                    sql =
                        $"INSERT INTO `t_user` " +
                        $"(`id`,`client_id`," +
                        $"`token`,`passphrase`,`playfab_id`, `device`)" +
                        $"VALUES(?, ?,  ?,?,?, ?);";
                    result_code = db?.Query(sql,
                        user_data.server_uid, user_data.client_uid,
                        user_data.token, user_data.passphrase,
                        user_data.custom_id, user_data.device);
                    if (result_code < 0)
                    {
                        return -1;
                    }
                }
                // 更新
                else
                {
                    int uid = (int)(db?.ResultItems["uid"]?.Number ?? -1);
                    int status = (int)(db?.ResultItems["status"]?.Number ?? 1);

                    user_data.server_uid = db?.ResultItems["server_id"]?.String ?? "";
                    if (user_data.server_uid.Length == 0)
                    {
                        return -1;
                    }

                    // 该账号不允许访问，已封禁
                    if (status == 0)
                    {
                        return -7;
                    }

                    //
                    privilege_level = (int)(db?.ResultItems["privilege_level"]?.Number ?? 0);

                    //
                    sql =
                        $"UPDATE `t_user` " +
                        $"SET " +
                        $"    `token` = ?, `passphrase` = ?, " +
                        $"    `device` = ?, `last_time` = NOW() " +
                        $"WHERE `id` = ? AND `uid` = ?;";
                    result_code = db?.Query(sql,
                        user_data.token, user_data.passphrase, user_data.device,
                        user_data.server_uid, uid);

                }

                //
                if (privilege_level >= 7)
                {
                    sql =
                        $"SELECT uid, id AS server_id, name, last_time, privilege_level, status " +
                        $"FROM t_admin WHERE id = ? AND status > 0;";
                    result_code = db?.Query(sql, user_data.server_uid);
                    if (result_code <= 0)
                    {
                        return -1;
                    }

                    privilege_level = (int)(db?.ResultItems["privilege_level"]?.Number ?? 0);
                    user_data.privilege_level = privilege_level;
                }

                //
                return 1;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBInitHOL(UserAuthenticationData user_data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql =
                    $"SELECT uid, id AS server_id, value, last_time, status " +
                    $"FROM t_hol WHERE id = ? AND status >= 0;";
                var result_code = db?.Query(sql, user_data.server_uid);
                if (result_code < 0)
                {
                    return -1;
                }

                // 不存在
                // 自动创建新纪录
                else if (result_code == 0)
                {
                    sql =
                        $"INSERT INTO `t_hol` " +
                        $"(`id`,`value`)" +
                        $"VALUES(?, ?);";
                    result_code = db?.Query(sql,
                        user_data.server_uid, 100);
                    if (result_code < 0)
                    {
                        return -1;
                    }
                }

                return 1;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected int DBGetUserProfile(UserBase user, string user_uid, out DBUserProfile? profile)
        {
            profile = null;

            var db = DatabaseManager.Instance.New();
            try
            {
                // 已经封了的用户是无法获取信息的
                string sql =
                    $"SELECT " +
                    $"    `uid`as nid, " +
                    $"    `id` as uid, " +
                    $"    `name`,`gender`,`region`, " +
                    $"    `avatar` as avatar_id, " +
                    $"    `create_time`, `last_time`, " +
                    $"    `status` " +
                    $"FROM `t_user` " +
                    $"WHERE id = ? AND status > 0;";
                var result_code = db?.Query(sql, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

                //
                profile = db?.ResultItems.To<DBUserProfile>();
                if (profile == null)
                {
                    return -1;
                }

                //
                return 1;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected int DBGetUserProfileExtend(UserBase user, UserProfile user_profile, out DBUserProfileExtend? profile)
        {
            profile = null;

            var db = DatabaseManager.Instance.New();
            try
            {
                // 已经封了的用户是无法获取信息的
                string sql =
                    $"SELECT " +
                    $"    `uid`as nid, " +
                    $"    `id` as uid, " +
                    $"    `last_rank_level`, `last_rank_value`, `rank_level`, `rank_value`,  " +
                    $"    `challenger_reals`, `season`, `season_time`, " +
                    $"    `create_time`, `last_time`, " +
                    $"    `status` " +
                    $"FROM `t_hol` " +
                    $"WHERE id = ? AND status > 0;";
                var result_code = db?.Query(sql, user_profile.UID);
                if (result_code < 0)
                {
                    return -1;
                }

                //
                profile = db?.ResultItems.To<DBUserProfileExtend>();
                if (profile == null)
                {
                    return -1;
                }

                //
                return 1;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }


        #region Inventory

        /// <summary>
        /// 数据库结果集转换为物品列表
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected Dictionary<string, UserInventoryItem> ToUserInventoryItems(List<DatabaseResultItemSet>? rows)
        {
            Dictionary<string, UserInventoryItem> items = new Dictionary<string, UserInventoryItem>(StringComparer.OrdinalIgnoreCase);
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    UserInventoryItem? inventory_item = v.To<UserInventoryItem>();
                    if (inventory_item == null) { continue; }

                    DatabaseResultItem data;
                    if (v.TryGetValue("custom_data", out data))
                    {
                        string text = data.AsString("");
                        var attributes = ItemUtils.ParseAttributeValues(text);
                        inventory_item.InitAttributes(attributes);
                    }
                    items.Add(inventory_item.iid, inventory_item);
                }
            }
            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBGetUserInventoryItems(DatabaseQuery? query, string user_uid,
                            List<UserInventoryItem> items, int type = -1)
        {
            //
            List<DatabaseResultItemSet>? list = null;
            // 
            string sql =
                $"SELECT " +
                $"    `uid`, id AS `iid`, tid AS `index`, " +
                $"    `user_id` AS `server_uid`, " +
                $"    `name`, `create_time`, `expired_time`, `remaining_time`, `using_time`, " +
                $"    `count`, `custom_data`, `status` " +
                $"FROM game.t_inventory AS i " +
                $"WHERE " +
                $" ((? >= 0 AND `type` = ?) OR (? < 0 AND `type` >= 0)) AND " +
                $" `user_id` = ? AND `status` > 0;";
            var result_code = query?.QueryWithList(sql, out list,
                type, type, type,
                user_uid);
            if (result_code < 0 || list == null)
            {
                return -1;
            }

            items.AddRange(this.ToUserInventoryItems(list).Values);
            return items.Count;
        }

        /// <summary>
        /// 目前更新包含
        ///    - 自定义属性
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateUserInventoryItems(DatabaseQuery? query, string user_uid,
                            List<UserInventoryItem> items)
        {
            foreach (var item in items)
            {
                string? attributes = item.GetAttributes();
                // 
                string sql =
                $"UPDATE `t_inventory` " +
                $"SET " +
                $"  `custom_data` = ? , `last_time` = CURRENT_TIMESTAMP " +
                $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                var result_code = query?.Query(sql,
                    attributes,
                    item.iid, item.index, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }
            }
            return 1;
        }


        protected async Task<int> DBUsingUserInventoryItem(DatabaseQuery? query, string user_uid,
                            UserInventoryItem item, bool is_using = true)
        {
            // 
            string sql =
                $"UPDATE `t_inventory` " +
                $"SET " +
                $"  `using_time` = ?, `last_time` = CURRENT_TIMESTAMP " +
                $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
            var result_code = query?.Query(sql,
                !is_using ? null : DateTime.Now,
                item.iid, item.index, user_uid);
            if (result_code < 0)
            {
                return -1;
            }
            return 1;
        }

        /// <summary>
        /// 更新物品自定义数据
        /// </summary>
        /// <returns></returns>
        protected async Task<int> DBUpdateUserInventoryItemCustomData(string user_uid, List<UserInventoryItem> items)
        {
            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();
                int result_code = await DBUpdateUserInventoryItems(db, user_uid, items);
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }
                db?.Commit();
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        /// <summary>
        /// 获取物品列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBGetUserInventoryItems(string user_uid, List<UserInventoryItem> items, int type = -1)
        {

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                int result_code = await DBGetUserInventoryItems(db, user_uid, items, type);
                if (result_code < 0)
                {
                    return -1;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }



        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBUsingUserInventoryItem(string user_uid, string item_iid,
                            Game.TItems item_template_data,
                            List<UserInventoryItem>? items)
        {
            if (items == null)
            {
                items = new List<UserInventoryItem>();
            }

            items.Clear();

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 获取该类所有物品
                List<UserInventoryItem> list = new List<UserInventoryItem>();
                int result_code = await DBGetUserInventoryItems(db, user_uid, list, item_template_data.Type);
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                var using_item = list.FirstOrDefault(v => v.iid == item_iid && v.index == item_template_data.Id);
                if (list.Count == 0 || using_item == null)
                {
                    db?.Rollback();
                    return 0;
                }

                // 物品已经在使用中，无需重复使用
                if (using_item.using_time != null)
                {
                    db?.Rollback();
                    return 1;
                }
                items.Add(using_item);

                // 更新使用物品
                if (await DBUsingUserInventoryItem(db, user_uid, using_item) < 0)
                {
                    db?.Rollback();
                    return -1;
                }
                using_item.using_time = DateTime.Now;


                // 移除已经在使用的物品
                var using_item_list = list.Where(v => v.index == item_template_data.Id && v.using_time != null).ToList();
                using_item_list.Remove(using_item);

                foreach (var v in using_item_list)
                {
                    if (v.using_time == null) { continue; }

                    if (await DBUsingUserInventoryItem(db, user_uid, v, false) < 0)
                    {
                        db?.Rollback();
                        return -1;
                    }
                    v.using_time = null;

                    items.Add(v);
                }

                db?.Commit();

            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 7;
        }

        /// <summary>
        /// 获取物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBGetUserInventoryItem(string user_uid, string item_iid,
                            Game.TItems item_template_data,
                            List<UserInventoryItem> items)
        {
            items.Clear();

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                // 获取该类所有物品
                List<UserInventoryItem> list = new List<UserInventoryItem>();
                int result_code = await DBGetUserInventoryItems(db, user_uid, list, item_template_data.Type);
                if (result_code < 0)
                {
                    return -1;
                }

                var item = list.FirstOrDefault(v => v.iid == item_iid && v.index == item_template_data.Id);
                if (list.Count == 0 || item == null)
                {
                    return 0;
                }

                //
                items.Add(item);
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        #region Inventory Internal
        /// <summary>
        /// 物品添加，没有做数据查询回滚，这里设置为私有函数
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddUserInventoryItems(DatabaseQuery? query, string user_uid, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (query == null)
            {
                return -1;
            }

            foreach (var item in items)
            {
                // 过期时间
                DateTime? expired = null;
                var template_data = item.GetTemplateData<TItems>();
                if (template_data?.Expired > 0)
                {
                    expired = DateTime.Now.AddSeconds(template_data?.Expired ?? 0);
                }

                // 
                string sql =
                    $"INSERT INTO `t_inventory` " +
                    $"  (`id`,`tid`,`name`, `type`, `user_id`, " +
                    $"  `create_time`, `last_time`, `expired_time`, `remaining_time`, `using_time`, " +
                    $"  `custom_data`, " +
                    $"  `status`) " +
                    $"VALUES " +
                    $"(?, ?, ?, ?, ?, " +
                    $"CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,?,NULL,NULL, " +
                    $"NULL,1); ";
                int result_code = query.Query(sql,
                        item.IID, item.ID,
                        item.GetTemplateData<TItems>()?.Name ?? "",
                        item.GetTemplateData<TItems>()?.Type ?? 0,
                        user_uid,
                        expired);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }

        /// <summary>
        /// 更新物品
        ///   更新参数
        ///     - 数量
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateUserInventoryItems(DatabaseQuery? query, string user_uid, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (query == null)
            {
                return -1;
            }

            // 如果名字有变更将更新
            foreach (var item in items)
            {
                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET `name` = ?, `type` = ?, `count` = ? " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.GetTemplateData<TItems>()?.Name ?? "",
                        item.GetTemplateData<TItems>()?.Type ?? 0,
                        item.Count,
                        item.IID, item.ID, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }

        /// <summary>
        /// 更新物品数量
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateUserInventoryItemsRemainingUses(DatabaseQuery? query, string user_uid, List<UserInventoryItem> items)
        {
            if (query == null)
            {
                return -1;
            }

            // 如果名字有变更将更新
            foreach (var item in items)
            {
                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET `count` = ? " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.count,
                        item.iid, item.index, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }

        protected async Task<int> DBUpdateUserInventoryItemsRemainingUses(DatabaseQuery? query, string user_uid, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (query == null)
            {
                return -1;
            }

            // 如果名字有变更将更新
            foreach (var item in items)
            {
                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET `count` = ? " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.Count,
                        item.IID, item.ID, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }

        /// <summary>
        /// 废除物品
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBRevokeUserInventoryItems(DatabaseQuery? query, string user_uid,
                                List<UserInventoryItem> items, string reason = "none")
        {
            if (query == null)
            {
                return -1;
            }

            // 
            foreach (var item in items)
            {
                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET `status` = 0 " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.iid, item.index, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }


        protected async Task<int> DBRevokeUserInventoryItems(DatabaseQuery? query, string user_uid,
                                List<AMToolkits.Game.GeneralItemData> items, string reason = "none")
        {
            if (query == null)
            {
                return -1;
            }

            // 
            foreach (var item in items)
            {
                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET `status` = 0 " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.IID, item.ID, user_uid);
                if (result_code < 0)
                {
                    return -1;
                }

            }

            return items.Count;
        }

        /// <summary>
        /// 增加物品列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> _DBAddUserInventoryItems(string user_uid, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                //
                var template_data = AMToolkits.Utility.TableDataManager.GetTableData<TItems>();

                List<UserInventoryItem> inventory_items = new List<UserInventoryItem>();
                if (await DBGetUserInventoryItems(user_uid, inventory_items) < 0)
                {
                    return -1;
                }

                Dictionary<string, UserInventoryItem> valided = inventory_items.ToDictionary(v => v.iid, v => v);

                //
                List<AMToolkits.Game.GeneralItemData> updated = new List<AMToolkits.Game.GeneralItemData>();
                List<AMToolkits.Game.GeneralItemData> added = new List<AMToolkits.Game.GeneralItemData>();
                foreach (var v in items)
                {
                    if (valided.ContainsKey(v.IID))
                    {
                        updated.Add(v);
                    }
                    else
                    {
                        added.Add(v);
                    }

                    v.InitTemplateData(template_data?.Get(v.ID));
                }


                if (await DBAddUserInventoryItems(db, user_uid, added) < 0 ||
                    await DBUpdateUserInventoryItems(db, user_uid, updated) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                db?.Commit();
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (AddUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        /// <summary>
        /// 更新物品列表 (完整)
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> _DBUpdateUserInventoryItems(string user_uid, List<AMToolkits.Game.GeneralItemData> items)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();
                //1. 获取用户背包物品
                //
                List<DatabaseResultItemSet>? list = null;
                // 
                string sql =
                    $"SELECT " +
                    $"    `uid`, id AS `iid`, tid AS `index`, " +
                    $"    `user_id` AS `server_uid`, " +
                    $"    `name`, `create_time`, `expired_time`, `remaining_time`, `using_time`, " +
                    $"    `custom_data`, `status` " +
                    $"FROM game.t_inventory AS i " +
                    $"WHERE `user_id` = ? AND `status` >= 0;";
                var result_code = db?.QueryWithList(sql, out list, user_uid);
                if (result_code < 0 || list == null)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                Dictionary<string, UserInventoryItem> valided = ToUserInventoryItems(list);

                List<UserInventoryItem> revoked = new List<UserInventoryItem>();
                List<AMToolkits.Game.GeneralItemData> updated = new List<AMToolkits.Game.GeneralItemData>();
                List<AMToolkits.Game.GeneralItemData> added = new List<AMToolkits.Game.GeneralItemData>();

                var template_data = AMToolkits.Utility.TableDataManager.GetTableData<TItems>();
                foreach (var v in items)
                {
                    UserInventoryItem? item = null;
                    if (valided.TryGetValue(v.IID, out item) && item != null)
                    {
                        // 已经删除的物品
                        if (item.status == 0)
                        {
                            _logger?.LogWarning($"{TAGName} (UpdateUserInventoryItems) (User:{user_uid}) {item.iid} - {item.index} - {item.name} ignore, " +
                                    $"The database has been deleted");
                            continue;
                        }
                        else
                        {
                            updated.Add(v);
                        }
                    }
                    else
                    {
                        added.Add(v);
                    }

                    v.InitTemplateData(template_data?.Get(v.ID));
                }

                foreach (var kvp in valided)
                {
                    if (!items.Any(v => v.IID == kvp.Key))
                    {
                        revoked.Add(kvp.Value);
                    }
                }

                if (await DBAddUserInventoryItems(db, user_uid, added) < 0 ||
                    await DBUpdateUserInventoryItems(db, user_uid, updated) < 0 ||
                    await DBRevokeUserInventoryItems(db, user_uid, revoked) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                db?.Commit();
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        /// <summary>
        /// 消耗物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> _DBConsumableUserInventoryItem(string user_uid,
                            List<AMToolkits.Game.GeneralItemData> items)
        {
            if (items.Count == 0)
            {
                return 0;
            }


            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                //
                List<AMToolkits.Game.GeneralItemData> updated = new List<AMToolkits.Game.GeneralItemData>();
                List<AMToolkits.Game.GeneralItemData> revoked = new List<AMToolkits.Game.GeneralItemData>();
                foreach (var v in items)
                {
                    if (v.Count > 0)
                    {
                        updated.Add(v);
                    }
                    else
                    {
                        revoked.Add(v);
                    }
                }

                // 获取该类所有物品
                if (await DBRevokeUserInventoryItems(db, user_uid, revoked, "consumable") < 0 ||
                    await DBUpdateUserInventoryItemsRemainingUses(db, user_uid, updated) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();

            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 7;
        }

        #endregion
        #endregion


        #region Game Events


        /// <summary>
        /// 数据库结果集转换为事件列表
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected Dictionary<int, GameEventItem> ToGameEvents(List<DatabaseResultItemSet>? rows)
        {
            Dictionary<int, GameEventItem> items = new Dictionary<int, GameEventItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    GameEventItem? event_item = v.To<GameEventItem>();
                    if (event_item == null) { continue; }

                    DatabaseResultItem data;
                    if (v.TryGetValue("items", out data))
                    {
                        string text = data.AsString("");
                        var list = ItemUtils.ParseGeneralItem(text);
                        event_item.InitGeneralItems(list);
                    }
                    items.Add(event_item.id, event_item);
                }
            }
            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected async Task<int> DBGetGameEvents(DatabaseQuery? query, string user_uid,
                            List<GameEventItem> items, int type = -1)
        {
            //
            List<DatabaseResultItemSet>? list = null;
            // 
            string sql =
                $"SELECT " +
                $"    `uid`, id AS `id`, `name`, `type` as `event_type`, " +
                $"    `user_id` AS `server_uid`, " +
                $"    `create_time`, `last_time`, `completed_time`, " +
                $"    `count`, `items`, `status` " +
                $"FROM `t_gameevents` " +
                $"WHERE " +
                $" ((? >= 0 AND `type` = ?) OR (? < 0 AND `type` >= 0)) AND " +
                $" `user_id` = ? AND `status` > 0;";
            var result_code = query?.QueryWithList(sql, out list,
                type, type, type,
                user_uid);
            if (result_code < 0 || list == null)
            {
                return -1;
            }

            items.AddRange(this.ToGameEvents(list).Values);
            return items.Count;
        }
        
        /// <summary>
        /// 添加，没有做数据查询回滚，这里设置为私有函数
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddGameEvent(DatabaseQuery? query, string user_uid,
                            GameEventItem item)
        {
            if (query == null)
            {
                return -1;
            }

            // 
            string sql =
                $"INSERT INTO `t_gameevents` " +
                $"  (`id`,`name`, `type`, `user_id`, " +
                $"  `create_time`, `last_time`, `completed_time`, " +
                $"  `items`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, ?, " +
                $"CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,NULL, " +
                $"NULL,1); ";
            int result_code = query.Query(sql,
                    item.id,
                    item.GetTemplateData<TGameEvents>()?.Name ?? "",
                    item.GetTemplateData<TGameEvents>()?.EventType ?? 0,
                    user_uid);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// 目前更新包含
        ///    
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateGameEvent(DatabaseQuery? query, string user_uid,
                            GameEventItem item, NGameEventData data)
        {
            var template_data = item.GetTemplateData<Game.TGameEvents>();

            DateTime? completed_time = null;

            item.count = item.count + data.Count;
            if (template_data != null)
            {
                // 计算更新数量
                if (template_data.RequireCount > 0 && item.count >= template_data.RequireCount)
                {
                    item.count = template_data.RequireCount;
                }

                if (template_data.RequireCount == item.count)
                {
                    completed_time = DateTime.Now;
                }
            }

            // 
            string sql =
            $"UPDATE `t_gameevents` " +
            $"SET " +
            $" `name` = ?,`count` = ?, " +
            $" `completed_time` = ? , `last_time` = CURRENT_TIMESTAMP " +
            $"WHERE `id` = ? AND `user_id` = ? ";
            var result_code = query?.Query(sql,
                template_data?.Name ?? data.Name, item.count,
                completed_time,
                data.ID, user_uid);
            if (result_code < 0)
            {
                return -1;
            }
            return 1;
        }
        
        /// <summary>
        /// 更新事件物品
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="id"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateGameEventItemData(DatabaseQuery? query, string user_uid,
                                int id, // 事件ID，不是UID
                                AMToolkits.Game.GeneralItemData[]? items)
        {
            if (query == null)
            {
                return -1;
            }

            string? values = null;
            if (items != null && items.Length > 0)
            {
                values = string.Join("|", items.Select(v => $"{v.ID},{v.Count},{v.IID}").ToList());
            }

            // 
            string sql =
            $"UPDATE `t_gameevents` " +
            $"SET " +
            $" `items` = ? " +
            $"WHERE `id` = ? AND `user_id` = ? AND `status` > 0 ";
            int result_code = query.Query(sql,
                    values,
                    id, user_uid);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }


        public async Task<int> _DBUpdateGameEventData(string user_uid,
                            NGameEventData data)
        {
            // 事件必须存在
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return -1;
            }
            // 
            var template_item = template_data.First(v => v.Id == data.ID);
            if (template_item == null)
            {
                return -1;
            }

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();


                List<GameEventItem> list = new List<GameEventItem>();
                if (await DBGetGameEvents(db, user_uid, list, -1) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                var item = list.FirstOrDefault(v => v.id == data.ID);
                if (item == null)
                {
                    item = new GameEventItem()
                    {
                        uid = -1,
                        id = data.ID,
                        name = data.Name,
                        count = data.Count,
                        event_type = template_item.EventType,
                        create_time = DateTime.Now,
                        last_time = DateTime.Now,
                        status = 1
                    };
                    item.InitTemplateData<Game.TGameEvents>(template_item);

                    if (await DBAddGameEvent(db, user_uid, item) < 0)
                    {
                        db?.Rollback();
                        return -1;
                    }
                }
                else
                {
                    item.InitTemplateData<Game.TGameEvents>(template_item);

                    // 已经完成，不再处理
                    if (item.count >= template_item.RequireCount || item.completed_time != null)
                    {
                        db?.Rollback();
                        return 0;
                    }
                }

                if (await DBUpdateGameEvent(db, user_uid, item, data) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                db?.Commit();
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (UpdateGameEventData) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        public async Task<int> _DBUpdateGameEventItemData(string user_uid,
                            NGameEventData data, List<AMToolkits.Game.GeneralItemData> items)
        {
            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                if (await DBUpdateGameEventItemData(db, user_uid, data.ID, items.ToArray()) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();

            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (DBUpdateGameEventItemData) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        #endregion

        #region CashShop

        protected async Task<int> DBAddCashshopItem(DatabaseQuery? query, string user_uid,
                                TShop template_data, PFNCashShopItemData data)
        {
            if (query == null)
            {
                return -1;
            }

            // 
            string sql =
                $"INSERT INTO `t_cashshop_items` " +
                $"  (`id`,`product_id`,`name`, `type`, `user_id`, `custom_id`, " +
                $"  `create_time`, `custom_data`, " +
                $"  `count`, `balance`, `amount`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, ?, ?, ?, " +
                $"CURRENT_TIMESTAMP,NULL, " +
                $"?, ?, ?, " +
                $"1); ";
            int result_code = query.Query(sql,
                    data.NID, data.ProductID,
                    template_data.Name, 0, user_uid, data.PlayFabUID,
                    1, data.Balance, data.Amount);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }
        
        protected async Task<int> DBUpdateCashshopItemData(DatabaseQuery? query, string user_uid,
                                string nid, string product_id, 
                                int index,
                                AMToolkits.Game.GeneralItemData data)
        {
            if (query == null || index >= 3)
            {
                return -1;
            }

            // 
            string sql =
                $"UPDATE `t_cashshop_items` SET " +
                $"  `item_{index}` = ? " +
                $"WHERE `id` = ? AND `product_id` = ? AND `user_id` = ?;";
            int result_code = query.Query(sql,
                    $"{data.ID},{data.Count},{data.IID}",
                    nid, product_id, user_uid);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }

        public async Task<int> _DBAddCashshopItems(string user_uid,
                            PFNCashShopItemData data)
        {
            if (data.ItemList == null || data.ItemList?.Length == 0)
            {
                return 0;
            }

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<TShop>();
            var template_item = template_data?.First(v => v.ProductId == data.ProductID);
            if (template_item == null)
            {
                return -1;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();


                List<UserInventoryItem> inventory_items = new List<UserInventoryItem>();
                if (await DBAddCashshopItem(db, user_uid, template_item, data) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                int index = 0;
                foreach (var item in data.ItemList)
                {
                    if (await DBUpdateCashshopItemData(db, user_uid, data.NID, data.ProductID,
                            index, item) < 0)
                    {
                        db?.Rollback();
                        return -1;
                    }
                    index++;
                }

                //
                db?.Commit();
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (AddCashshopItems) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        #endregion
    }
}