//// 新增设备纪录
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using AMToolkits.Game;

using Game;
using Logger;


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
        /// 未使用
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl = "";

        [JsonPropertyName("changed_time")]
        public DateTime? ChangedTime = null;
    }


    [System.Serializable]
    public class DBUserProfileExtend 
    {
        [JsonPropertyName("nid")]
        public int NID = -1;

        [JsonPropertyName("uid")]
        public string UID = "";

        /// <summary>
        /// 用户等级（不是账号等级）
        /// </summary>
        [JsonPropertyName("level")]
        public int Level = 0;
        /// <summary>
        /// 用户经验
        /// </summary>
        [JsonPropertyName("experience")]
        public int Experience = 0;

        /// <summary>
        /// 隐藏分
        /// </summary>
        [JsonPropertyName("cp_value")]
        public int CPValue = 0;

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
        /// 当前赛季 段位 最好
        /// </summary>
        [JsonPropertyName("rank_level_best")]
        public int RankLevelBest = 0;

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

        /// <summary>
        /// 玩家游戏局数
        /// </summary>
        [JsonPropertyName("played_count")]
        public int PlayedCount = 0;

        /// <summary>
        /// 玩家获胜游戏局数
        /// </summary>
        [JsonPropertyName("played_win_count")]
        public int PlayedWinCount = 0;
        
        /// <summary>
        /// 玩家游戏局数
        /// </summary>
        [JsonPropertyName("season_played_count")]
        public int SeasonPlayedCount = 0;

        /// <summary>
        /// 玩家获胜游戏局数
        /// </summary>
        [JsonPropertyName("season_played_win_count")]
        public int SeasonPlayedWinCount = 0;
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
            user_data.is_new_user = false;

            string? link_id = null;
            if(user_data.link_name?.Trim().IsNullOrWhiteSpace() == false)
            {
                link_id = $"{user_data.link_name}_openid_{user_data.link_id}";
            }
            var db = DatabaseManager.Instance.New();
            try
            {
                int privilege_level = 0;

                // PlayFab 
                string sql =
                    $"SELECT uid, id AS server_id, client_id, " +
                    $"  name, " +
                    $"  token, passphrase, last_time, privilege_level, " +
                    $"  link_id, " +
                    $"  status " +
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
                        $"  `token`,`passphrase`,`playfab_id`, `device`, " +
                        $"  `link_id` " +
                        $")" +
                        $"VALUES(?, ?,  ?,?,?, ?, ?);";
                    result_code = db?.Query(sql,
                        user_data.server_uid, user_data.client_uid,
                        user_data.token, user_data.passphrase,
                        user_data.custom_id, user_data.device,
                        link_id);
                    if (result_code < 0)
                    {
                        return -1;
                    }

                    user_data.is_new_user = true;
                }
                // 更新
                else
                {
                    int uid = (int)(db?.ResultItems["uid"]?.Number ?? -1);
                    string? name = (db?.ResultItems["name"]?.String);
                    int status = (int)(db?.ResultItems["status"]?.Number ?? 1);

                    user_data.name = name;
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
                    string? openid = (db?.ResultItems["link_id"]?.String);
                    if(!openid.IsNullOrWhiteSpace() && openid != link_id)
                    {
                        return -5;
                    }

                    //
                    privilege_level = (int)(db?.ResultItems["privilege_level"]?.Number ?? 0);

                    //
                    sql =
                        $"UPDATE `t_user` " +
                        $"SET " +
                        $"    `token` = ?, `passphrase` = ?, " +
                        $"    `device` = ?, `last_time` = NOW(), " +
                        $"    `link_id` = ? " +
                        $"WHERE `id` = ? AND `uid` = ?;";
                    result_code = db?.Query(sql,
                        user_data.token, user_data.passphrase, user_data.device,
                        link_id,
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

        #region HOL
        protected int DBInitHOL(UserAuthenticationData user_data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql =
                    $"SELECT `uid`, id AS server_id, `value`, `last_time`, `status` " +
                    $"FROM t_hol " +
                    $"WHERE `id` = ? AND `season` = ? AND `status` >= 0;";
                var result_code = db?.Query(sql, user_data.server_uid,
                    GameSettingsInstance.Settings.Season.Code);
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
                        $"(`id`,`value`, `season`)" +
                        $"VALUES(?, ?, ?);";
                    result_code = db?.Query(sql,
                        user_data.server_uid, 100,
                        GameSettingsInstance.Settings.Season.Code);
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


        protected int DBGetHOLData(string user_id, out UserHOLData? data)
        {
            data = null;

            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql =
                    $"SELECT  " +
                    $"  h.`uid`, h.`id`, u.`name`, " +
                    $"  h.`level`, h.`experience`, " +
                    $"  h.`value`, " +
                    $"  `cp_value`, `played_count`, `played_win_count`, `winning_streak_count`, `winning_streak_highest`, " +
                    $"  `season_played_count`, `season_played_win_count`, `season_winning_streak_count`, `season_winning_streak_highest`, "+
                    $"  `season`, `season_time`, `challenger_reals`, " +
                    $"  `last_rank_level`, `last_rank_value`, `rank_level`, `rank_value`, `rank_level_best`, " +
                    $"  h.`create_time`, h.`last_time`, " +
                    $"  h.`status`  " +
                    $"FROM `t_hol` AS h " +
                    $"LEFT JOIN `t_user` AS u ON u.`id` = h.`id` AND u.`status` > 0 " +
                    $"WHERE h.`id` = ? AND h.`season` = ? AND h.`status` > 0";
                var result_code = db?.Query(sql, user_id,
                    GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return -1;
                }

                //
                data = db?.ResultItems.To<UserHOLData>();
                if (data == null)
                {
                    return -1;
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
        /// 仅仅更新等级和经验
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected int DBUpdateHOLData(string user_id, UserHOLData data)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql =
                    $"UPDATE `t_hol` AS h SET " +
                    $"   " +
                    $"  h.`level` = ?, h.`experience` = ? " +
                    $"WHERE h.`id` = ? AND h.`season` = ? AND h.`status` > 0; ";
                var result_code = db?.Query(sql,
                    data.level, data.experience,
                    user_id,
                    GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return -1;
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


        #endregion HOL

        #region Profile
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="target_user_uid"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected int DBGetUserProfile(string user_id, string target_user_uid, out DBUserProfile? profile)
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
                    $"    `avatar` as avatar_url, " +
                    $"    `create_time`, `last_time`, `changed_time`, " +
                    $"    `status` " +
                    $"FROM `t_user` " +
                    $"WHERE id = ? AND status > 0;";
                var result_code = db?.Query(sql, target_user_uid);
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

                // 早期库默认设置
                if (profile.AvatarUrl == null || profile.AvatarUrl.Trim() == "0" || profile.AvatarUrl.Trim() == "null")
                {
                    profile.AvatarUrl = "";
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
        /// <param name="user_uid">当前用户</param>
        /// <param name="user_profile"></param>
        /// <returns></returns>
        protected async Task<DBUserProfileExtend?> DBGetUserProfileExtend(string user_id, UserProfile user_profile)
        {
            DBUserProfileExtend? profile = null;

            var db = DatabaseManager.Instance.New();
            try
            {
                // 已经封了的用户是无法获取信息的
                string sql =
                    $"SELECT " +
                    $"    `uid`as nid, " +
                    $"    `id` as uid, " +
                    $"    `level`, `experience`, `cp_value`, " +
                    $"    `last_rank_level`, `last_rank_value`, `rank_level`, `rank_value`, `rank_level_best`, " +
                    $"    `challenger_reals`, `season`, `season_time`, " +
                    $"    `played_count`, `played_win_count`, `season_played_count`, `season_played_win_count`, " +
                    $"    `create_time`, `last_time`, " +
                    $"    `status` " +
                    $"FROM `t_hol` " +
                    $"WHERE `id` = ? AND `season` = ? AND `status` > 0;";
                var result_code = db?.Query(sql, user_profile.UID,
                        GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return null;
                }

                //
                profile = db?.ResultItems.To<DBUserProfileExtend>();
                if (profile == null)
                {
                    return null;
                }

                //
                return profile;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return null;
        }

        protected async Task<int> DBUpdateUserProfile(UserBase user, DBUserProfile? profile, UserProfile to_profile)
        {
            if (profile == null)
            {
                return -1;
            }

            var parameters = new List<object?>();

            // 条件
            string condition_update_nickname = "";
            if (to_profile.Name.Length > 0)
            {
                condition_update_nickname = $" `name` = ?, ";
                parameters.Add(to_profile.Name);
            }

            parameters.Add(to_profile.AvatarUrl);

            string condition_update_gender = "";
            if (to_profile.Gender >= 0)
            {
                condition_update_gender = $" `gender` = ?, ";
                parameters.Add(to_profile.Gender);
            }

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                parameters.Add(profile.UID);
                //
                string sql =
                    $"UPDATE `t_user` " +
                    $"SET " +
                    $"    {condition_update_nickname} " +
                    $"    `avatar` = ?, " +
                    $"    {condition_update_gender} " +
                    $"    `updated_time` = CURRENT_TIMESTAMP " +
                    $"WHERE `id` = ? AND `status` > 0;";
                var result_code = db?.Query(sql, parameters.ToArray());
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();

                //
                profile.AvatarUrl = to_profile.AvatarUrl;
                profile.Name = to_profile.Name;
                profile.Gender = to_profile.Gender;

                //
                return 1;
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected async Task<int> DBChangeUserName(UserBase user, UserProfile profile, string to_name)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();
                // 已经封了的用户是无法获取信息的
                string sql =
                    $"SELECT " +
                    $"    `uid`as nid, " +
                    $"    `id` as uid, " +
                    $"    `name`,  " +
                    $"    `create_time`, `last_time`, " +
                    $"    `status` " +
                    $"FROM `t_user` " +
                    $"WHERE name = ?;";
                var result_code = db?.Query(sql, to_name);
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                // 已经存在，无路是否删除
                string? id = db?.ResultItems["id"]?.AsString("");
                if (!id.IsNullOrWhiteSpace())
                {
                    db?.Rollback();
                    return -3;
                }

                //
                sql =
                    $"UPDATE `t_user` " +
                    $"SET " +
                    $"    `name` = ?, `changed_time` = CURRENT_TIMESTAMP " +
                    $"WHERE `id` = ? AND `status` > 0;";
                result_code = db?.Query(sql,
                    to_name,
                    user.UID);
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();

                //
                return 1;
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
        #endregion


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
        /// <param name="ids">物品ID列表(可选)，默认为NULL</param>
        /// <returns></returns>
        protected async Task<int> DBGetUserInventoryItems(DatabaseQuery? query, string user_uid,
                            List<UserInventoryItem> items, int type = -1,
                            int[]? ids = null, int[]? groups = null)
        {
            //
            List<DatabaseResultItemSet>? list = null;

            // ids 条件
            string condition_case_ids = "";
            if (ids != null && ids.Length > 0)
            {
                condition_case_ids = $" AND (i.`tid` IN ({string.Join(",", ids)})) ";
            }
            string condition_case_groups = "";
            if (groups != null && groups.Length > 0)
            {
                condition_case_groups = $" AND (i.`group` IN ({string.Join(",", groups)})) ";
            }

            // 
            string sql =
                $"SELECT " +
                $"    `uid`, id AS `iid`, tid AS `index`, `group`, " +
                $"    `user_id` AS `server_uid`, " +
                $"    `name`, `create_time`, `expired_time`, `remaining_time`, `using_time`, " +
                $"    `count`, `custom_data`, `status` " +
                $"FROM `t_inventory` AS i " +
                $"WHERE " +
                $" ((? >= 0 AND `type` = ?) OR (? < 0 AND `type` >= 0)) " +
                $" {condition_case_ids} " +
                $" {condition_case_groups} " +
                $" AND `user_id` = ? AND `status` > 0;";
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
                            UserInventoryItem item, bool is_using = true, int count = 0)
        {
            // ids 条件
            string condition_update_count = "";
            if (count > 0)
            {
                condition_update_count = $" `count` = `count` - {count}, ";
            }

            // 
            string sql =
                $"UPDATE `t_inventory` " +
                $"SET " +
                $"  { condition_update_count } " +
                $"  `using_time` = ?,  `remaining_time` = ?, `last_time` = CURRENT_TIMESTAMP " +
                $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
            var result_code = query?.Query(sql,
                !is_using ? null : DateTime.Now,
                item.remaining_time,
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
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected async Task<int> DBGetUserInventoryItems(string user_uid,
                            IEnumerable<AMToolkits.Game.GeneralItemData> items,
                            List<UserInventoryItem> list)
        {
            //
            var db = DatabaseManager.Instance.New();
            try
            {
                var ids = items.Select(v => v.ID).ToArray();
                int result_code = await DBGetUserInventoryItems(db, user_uid, list, -1, ids);
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
        /// 使用装备
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBUsingUserInventoryItem(string user_uid, string item_iid,
                            Game.TItems template_data,
                            List<UserInventoryItem>? items)
        {
            items?.Clear();

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 获取该类所有物品
                int result_code = 0;
                List<UserInventoryItem> list = new List<UserInventoryItem>();
                if (template_data.Group > 0)
                {
                    result_code = await DBGetUserInventoryItems(db, user_uid, list, template_data.Type,
                            null, new int[] { template_data.Group });
                }
                else
                {
                    result_code = await DBGetUserInventoryItems(db, user_uid, list, template_data.Type,
                            new int[] { template_data.Id }, null);
                }
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                var using_item = list.FirstOrDefault(v => v.iid == item_iid && v.index == template_data.Id);
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
                items?.Add(using_item);

                // 更新使用物品
                if (await DBUsingUserInventoryItem(db, user_uid, using_item, true) < 0)
                {
                    db?.Rollback();
                    return -1;
                }
                using_item.using_time = DateTime.Now;


                // 移除已经在使用的物品
                var using_item_list = list.Where(v => v.index == template_data.Id && v.using_time != null).ToList();
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

                    items?.Add(v);
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
        /// 使用消耗品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="item_iid"></param>
        /// <param name="template_data"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBUsingUserInventoryItem(string user_uid, string item_iid,
                            Game.TItems template_data,
                            List<UserInventoryItem>? items,
                            int Count)
        {
            items?.Clear();

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 获取该类所有物品
                int result_code = 0;
                List<UserInventoryItem> list = new List<UserInventoryItem>();
                if (template_data.Group > 0)
                {
                    result_code = await DBGetUserInventoryItems(db, user_uid, list, template_data.Type,
                            null, new int[] { template_data.Group });
                }
                else
                {
                    result_code = await DBGetUserInventoryItems(db, user_uid, list, template_data.Type,
                            new int[] { template_data.Id }, null);
                }
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                var using_item = list.FirstOrDefault(v => v.iid == item_iid && v.index == template_data.Id);
                if (list.Count == 0 || using_item == null)
                {
                    db?.Rollback();
                    return 0;
                }
                // 物品数量不够使用
                if (using_item.count - Count < 0)
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

                items?.Add(using_item);

                // 已经有其他同类型的物品再使用
                var exist_item = list.FirstOrDefault(v =>
                    (v.index == template_data.Id && v.using_time != null) ||
                    (v.group > 0 && v.using_time != null));
                if (exist_item != null)
                {
                    items?.Add(exist_item);
                    db?.Rollback();
                    return 1;
                }

                // 无论是否
                if (template_data.Remaining > 0) {
                    using_item.remaining_time = DateTime.Now.AddSeconds(template_data.Remaining);
                }

                // 更新使用物品
                // 此处不再更新数量
                if (await DBUsingUserInventoryItem(db, user_uid, using_item, true, 0) < 0)
                {
                    db?.Rollback();
                    return -1;
                }
                using_item.using_time = DateTime.Now;
                //using_item.count = System.Math.Max(using_item.count - Count, 0);

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
                    $"  (`id`,`tid`,`name`, `type`, `group`, `user_id`, " +
                    $"  `create_time`, `last_time`, `expired_time`, `remaining_time`, `using_time`, " +
                    $"  `custom_data`, " +
                    $"  `status`) " +
                    $"VALUES " +
                    $"(?, ?, ?, ?, ?, ?, " +
                    $"CURRENT_TIMESTAMP,CURRENT_TIMESTAMP, ?, NULL,NULL, " +
                    $"NULL,1); ";
                int result_code = query.Query(sql,
                        item.IID, item.ID,
                        item.GetTemplateData<TItems>()?.Name ?? "",
                        item.GetTemplateData<TItems>()?.Type ?? 0,
                        item.GetTemplateData<TItems>()?.Group ?? (int)AMToolkits.Game.GameGroupType.None,
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
                    $"SET `name` = ?, `type` = ?, `group` = ?, `count` = ? " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        item.GetTemplateData<TItems>()?.Name ?? "",
                        item.GetTemplateData<TItems>()?.Type ?? 0,
                        item.GetTemplateData<TItems>()?.Group ?? (int)AMToolkits.Game.GameGroupType.None,
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

        protected async Task<int> DBUpdateUserInventoryItemsRemainingUses(DatabaseQuery? query, string user_uid, List<AMToolkits.Game.GeneralItemData> items, 
                                    string reason = "none")
        {
            if (query == null)
            {
                return -1;
            }

            // 如果名字有变更将更新
            foreach (var item in items)
            {
                // 条件
                string condition_update = "";

                reason = reason.Trim().ToLower();
                if (reason == "using")
                {
                    if (item.Count > 0)
                    {
                        condition_update = $" `using_time` = NULL, `remaining_time` = NULL, ";
                    }
                }

                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET " +
                    $"  {condition_update} `count` = ? " +
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
                    $"SET " +
                    $" `delete_time` = CURRENT_TIMESTAMP, " +
                    $" `reason` = ?, `status` = 0 " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        reason,
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
                // ids 条件
                string condition_update_count = "";
                string condition_update = "";

                reason = reason.Trim().ToLower();
                if (reason == "using")
                {
                    if (item.Count == 0)
                    {
                        condition_update_count = $" `count` = 0, ";
                    }
                    // 消耗是按单件计算时间的
                    else
                    {
                        condition_update = $" `using_time` = NULL, `remaining_time` = NULL, ";
                    }
                }

                // 
                string sql =
                    $"UPDATE `t_inventory` " +
                    $"SET " +
                    $"  {condition_update_count} " +
                    $"  {condition_update} " +
                    $" `delete_time` = CURRENT_TIMESTAMP, " +
                    $"  `reason` = ?, `status` = 0 " +
                    $"WHERE `id` = ? AND `tid` = ? AND `user_id` = ? ";
                int result_code = query.Query(sql,
                        reason,
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
                            List<AMToolkits.Game.GeneralItemData> items, string reason = "")
        {
            if (reason.IsNullOrWhiteSpace())
            {
                reason = "consumable";
            }

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
                if (await DBRevokeUserInventoryItems(db, user_uid, revoked, reason) < 0 ||
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


        #region Rank
        protected async Task<UserRankDataExtend?> DBGetUserRank(string user_id)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql =
                    $"SELECT  " +
                    $"  h.`uid`, h.`id`, u.`name`, " +
                    $"  h.`value`, " +
                    $"  `cp_value`,  " +
                    $"  `played_count`, `played_win_count`, `season_played_count`, `season_played_win_count`, " +
                    $"  `season`, `season_time`, `challenger_reals`, " +
                    $"  `last_rank_level`, `last_rank_value`, `rank_level`, `rank_value`, `rank_level_best`, `rank_score`, " +
                    $"  `winning_streak_count`, `winning_streak_highest`, `season_winning_streak_count`, `season_winning_streak_highest`, " +
                    $"  h.`create_time`, h.`last_time`, " +
                    $"  h.`status`  " +
                    $"FROM `t_hol` AS h " +
                    $"LEFT JOIN `t_user` AS u ON u.`id` = h.`id` AND u.`status` > 0  " +
                    $"WHERE h.`id` = ? AND h.`season` = ? AND h.`status` > 0";
                var result_code = db?.Query(sql, user_id,
                        GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return null;
                }

                //
                var data = db?.ResultItems.To<UserRankDataExtend>();
                if (data == null)
                {
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                _logger?.LogError("(User) Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return null;
        }

        #endregion


        #region Game Effects

        /// <summary>
        /// 数据库结果集转换为特效列表
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected List<GameEffectItem> ToGameEffects(List<DatabaseResultItemSet>? rows)
        {
            List<GameEffectItem> items = new List<GameEffectItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    GameEffectItem? effect_item = v.To<GameEffectItem>();
                    if (effect_item == null) { continue; }

                    DatabaseResultItem data;
                    if (v.TryGetValue("effect_value", out data))
                    {
                        string text = data.AsString("");
                    }
                    items.Add(effect_item);
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
        protected async Task<int> DBGetGameEffects(DatabaseQuery? query, string user_uid,
                            List<GameEffectItem> items,
                            int type = -1, int group_index = -1)
        {

            // type 条件
            string condition_case_type = "";
            if (type >= 0)
            {
                condition_case_type = $" AND (e.`type` = {type}) ";
            }

            string condition_case_group_index = "";
            if (group_index >= 0)
            {
                condition_case_group_index = $" AND (e.`group` = {group_index}) ";
            }


            //
            List<DatabaseResultItemSet>? list = null;

            // 
            string sql =
                $"SELECT " +
                $"    `uid`, id AS `id`, `name`, `value`, " +
                $"    `type` as `effect_type`, `sub_type` AS `effect_sub_type`, `group` AS `group_index`, " +
                $"    `user_id` AS `server_uid`, " +
                $"    `create_time`, `last_time`, `end_time`, " +
                $"    `value` AS `effect_value`, `status` " +
                $"FROM `t_gameeffects` e " +
                $"WHERE " +
                $" `user_id` = ? AND `status` > 0 " +
                $" {condition_case_type} " +
                $" {condition_case_group_index} " +
                $" AND ((`end_time` IS NOT NULL AND `end_time` >= CURRENT_TIMESTAMP) OR (`end_time` IS NULL)) " +
                $" ;";
            var result_code = query?.QueryWithList(sql, out list,
                user_uid);
            if (result_code < 0 || list == null)
            {
                return -1;
            }

            items.AddRange(this.ToGameEffects(list));
            return items.Count;
        }

        /// <summary>
        /// 添加，没有做数据查询回滚，这里设置为私有函数
        ///     特效存在多个ID+UserID不能唯一化
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddGameEffect(DatabaseQuery? query, string user_uid,
                            GameEffectItem item)
        {
            if (query == null)
            {
                return -1;
            }

            // 
            string sql =
                $"INSERT INTO `t_gameeffects` " +
                $"  (`id`,`name`, `type`, `user_id`, `group`, " +
                $"  `create_time`, `last_time`, `end_time`, " +
                $"  `value`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, ?, ?, " +
                $"CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,NULL, " +
                $"NULL,1); ";
            int result_code = query.Query(sql,
                    item.id,
                    item.GetTemplateData<TGameEffects>()?.Name ?? "",
                    item.GetTemplateData<TGameEffects>()?.EffectType ?? 0,
                    user_uid,
                    item.GetTemplateData<TGameEffects>()?.Group ?? (int)AMToolkits.Game.GameGroupType.None
                    );
            if (result_code < 0)
            {
                return -1;
            }
            // 必须增加
            item.uid = (int)query.LastID;

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
        protected async Task<int> DBUpdateGameEffect(DatabaseQuery? query, string user_uid,
                            GameEffectItem item)
        {
            var template_data = item.GetTemplateData<Game.TGameEffects>();

            var end_time = item.end_time;
            
            // 
            string sql =
            $"UPDATE `t_gameeffects` " +
            $"SET " +
            $" `name` = ?, `group` = ?, " +
            $" `end_time` = ? " +
            $"WHERE `uid` = ? AND `id` = ? AND `user_id` = ? ";
            var result_code = query?.Query(sql,
                template_data?.Name ?? item.name,
                template_data?.Group ?? (int)AMToolkits.Game.GameGroupType.None,
                end_time,
                item.uid, item.id, user_uid);
            if (result_code < 0)
            {
                return -1;
            }
            return 1;
        }

        protected async Task<int> DBUpdateGameEffect(DatabaseQuery? query, string user_uid,
                            GameEffectItem item, string? link_items)
        {            
            // 
            string sql =
            $"UPDATE `t_gameeffects` " +
            $"SET " +
            $"  " +
            $"  `items` = ? " +
            $"WHERE `uid` = ? AND `id` = ? AND `user_id` = ? ";
            var result_code = query?.Query(sql,
                link_items,
                item.uid, item.id, user_uid);
            if (result_code < 0)
            {
                return -1;
            }
            return 1;
        }


        /// <summary>
        /// 获取特效列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBGetGameEffects(string user_uid, List<GameEffectItem> items,
                                    int type = -1,
                                    int group_index = -1)
        {

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEffects>();

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                int result_code = await DBGetGameEffects(db, user_uid, items, type, group_index);
                if (result_code < 0)
                {
                    return -1;
                }

                if(items.Count > 0)
                {
                    foreach(var item in items)
                    {
                        var template_item = template_data?.Get(item.id);
                        if(template_item != null)
                        {
                            item.InitTemplateData<TGameEffects>(template_item);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (GetGameEffects) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> _DBAddGameEffectData(string user_uid,
                            NGameEffectData data,
                            List<GameEffectItem> list)
        {
            // 事件必须存在
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEffects>();
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

                var item = new GameEffectItem()
                {
                    uid = -1,
                    id = data.ID,
                    name = data.Name,
                    effect_type = template_item.EffectType,
                    create_time = DateTime.Now,
                    last_time = DateTime.Now,
                    status = 1
                };
                item.InitTemplateData<Game.TGameEffects>(template_item);


                if (await DBAddGameEffect(db, user_uid, item) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                item.end_time = data.EndTime;

                //
                if (item.end_time != null)
                {
                    if (await DBUpdateGameEffect(db, user_uid, item) < 0)
                    {
                        db?.Rollback();
                        return -1;
                    }
                }

                //
                db?.Commit();

                //
                list.Add(item);
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


        public async Task<int> _DBUpdateGameEffectData(string user_uid,
                            GameEffectItem item,
                            List<UserInventoryItem> list)
        {
            if (list.Count == 0)
            {
                return 0;
            }

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                var link_items = string.Join(",", list.Select(v => $"{v.index}|IID{v.iid}").ToList());

                if (await DBUpdateGameEffect(db, user_uid, item, link_items) < 0)
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
        /// 获取事件列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> DBGetGameEvents(string user_uid, List<GameEventItem> items,
                                    int type = -1,
                                    int group_index = -1)
        {

            //
            var db = DatabaseManager.Instance.New();
            try
            {
                int result_code = await DBGetGameEvents(db, user_uid, items, type, group_index);
                if (result_code < 0)
                {
                    return -1;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (GetGameEvents) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
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
                            List<GameEventItem> items,
                            int type = -1, int group_index = -1, bool is_season = true)
        {
            
            // type 条件
            string condition_case_type = "";
            if (type >= 0)
            {
                condition_case_type = $" AND (e.`type` = {type}) ";
            }

            string condition_case_group_index = "";
            if (group_index >= 0)
            {
                condition_case_group_index = $" AND (e.`group` = {group_index}) ";
            }

            string condition_case = "";
            if (is_season)
            {
                condition_case = $" AND `season` = {GameSettingsInstance.Settings.Season.Code} ";
            }


            //
            List<DatabaseResultItemSet>? list = null;

            // 
            string sql =
                $"SELECT " +
                $"    `uid`, id AS `id`, `name`, `value`, " +
                $"    `type` as `event_type`, `sub_type` as `event_sub_type`, `group` as `group_index`, " +
                $"    `user_id` AS `server_uid`, " +
                $"    `create_time`, `last_time`, `completed_time`, " +
                $"    `count`, `items`, `season`, `status` " +
                $"FROM `t_gameevents` e " +
                $"WHERE " +
                $" `user_id` = ? AND `status` > 0 " +
                $" {condition_case_type} " +
                $" {condition_case_group_index} " +
                $" {condition_case} " +
                $" ;";
            var result_code = query?.QueryWithList(sql, out list,
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
                $"  (`id`,`name`, `type`, `user_id`, `group`, " +
                $"  `create_time`, `last_time`, `completed_time`, " +
                $"  `items`, " +
                $"  `season`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, ?, ?, " +
                $"CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,NULL, " +
                $"NULL, ?, 1); ";
            int result_code = query.Query(sql,
                    item.id,
                    item.GetTemplateData<TGameEvents>()?.Name ?? "",
                    item.GetTemplateData<TGameEvents>()?.EventType ?? 0,
                    user_uid,
                    item.GetTemplateData<TGameEvents>()?.Group ?? (int)AMToolkits.Game.GameGroupType.None,
                    GameSettingsInstance.Settings.Season.Code);
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
                            Game.TGameEvents template_data,
                            NGameEventData data)
        {
            // 
            string sql =
            $"UPDATE `t_gameevents` " +
            $"SET " +
            $" `name` = ?,`count` = ?, `group` = ?, " +
            $" `completed_time` = NULL , `last_time` = CURRENT_TIMESTAMP " +
            $"WHERE `id` = ? AND `user_id` = ? AND `season` = ? ";
            var result_code = query?.Query(sql,
                template_data?.Name ?? data.Name, data.Count,
                template_data?.Group ?? (int)AMToolkits.Game.GameGroupType.None,
                data.ID, user_uid,
                GameSettingsInstance.Settings.Season.Code);
            if (result_code < 0)
            {
                return -1;
            }
            return 1;
        }

        protected async Task<int> DBUpdateGameEvent(DatabaseQuery? query, string user_uid,
                            GameEventItem item)
        {
            var template_data = item.GetTemplateData<Game.TGameEvents>();

            // 
            string sql =
            $"UPDATE `t_gameevents` " +
            $"SET " +
            $" `name` = ?,`count` = ?, `group` = ?, `value` = ?, " +
            $" `completed_time` = ? , `last_time` = CURRENT_TIMESTAMP " +
            $"WHERE `id` = ? AND `user_id` = ? AND `season` = ? ";
            var result_code = query?.Query(sql,
                template_data?.Name ?? item.name, item.count,
                template_data?.Group ?? 0, item.value,
                item.completed_time,
                item.id, user_uid,
                GameSettingsInstance.Settings.Season.Code);
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
                values = string.Join(",", items.Select(v => $"{v.ID}|{v.Count}|IID{v.IID}").ToList());
            }

            // 
            string sql =
            $"UPDATE `t_gameevents` " +
            $"SET " +
            $" `items` = ? " +
            $"WHERE `id` = ? AND `user_id` = ? AND `season` = ? AND `status` > 0 ";
            int result_code = query.Query(sql,
                    values,
                    id, user_uid,
                    GameSettingsInstance.Settings.Season.Code);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }

        public async Task<int> _DBUpdateGameEventItem(string user_uid,
                            GameEventItem item)
        {
            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();


                List<GameEventItem> list = new List<GameEventItem>();
                if (await DBGetGameEvents(db, user_uid, list, item.event_type, item.group_index) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                var result = list.FirstOrDefault(v => v.id == item.id);
                if (result == null)
                {
                    if (await DBAddGameEvent(db, user_uid, item) < 0)
                    {
                        db?.Rollback();
                        return -1;
                    }
                }
                else
                {
                    item.count = item.count + result.count;
                    // 计算更新数量
                    if (item.GetTemplateData<Game.TGameEvents>()?.RequireCount > 0
                        && item.count >= item.GetTemplateData<Game.TGameEvents>()?.RequireCount)
                    {
                        item.count = item.GetTemplateData<Game.TGameEvents>()?.RequireCount ?? item.count;
                    }
                }

                if (await DBUpdateGameEvent(db, user_uid, item) < 0)
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
                _logger?.LogError($"{TAGName} (DBUpdateGameEventItem) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        public async Task<int> _DBUpdateGameEventItemData(string user_uid,
                            GameEventItem item, List<AMToolkits.Game.GeneralItemData> items)
        {
            //
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                if (await DBUpdateGameEventItemData(db, user_uid, item.id, items.ToArray()) < 0)
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

                    data.CreateTime = item.create_time;
                    data.LastTime = item.last_time;

                    // 已经完成，不再处理
                    if (item.IsCompleted)
                    {
                        data.CompletedTime = item.completed_time;

                        db?.Rollback();
                        return 0;
                    }
                }

                if (await DBUpdateGameEvent(db, user_uid, template_item, data) < 0)
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
                $"  `virtual_balance`, `virtual_currency`, `virtual_amount`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, ?, ?, ?, " +
                $"CURRENT_TIMESTAMP,NULL, " +
                $"?, ?, ?, " +
                $"?, ?, ?, " +
                $"1); ";
            int result_code = query.Query(sql,
                    data.NID, data.ProductID,
                    template_data.Name, 0, user_uid, data.PlayFabUID,
                    1, data.Balance, data.Amount,
                    data.CurrentBalance, data.CurrentVirtualCurrency, data.CurrentAmount);
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
                if (data.ItemList != null)
                {
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