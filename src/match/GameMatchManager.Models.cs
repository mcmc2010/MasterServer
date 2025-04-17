using Logger;

namespace Server
{
    [System.Serializable]
    public class GameMatchQueueItem
    {
        public string sn = "";
        public string server_id = "";
        public string name = "";
        public int hol_value = 0;
        public DateTime? create_time = null;
        public DateTime? last_time = null;
        public int wait_time = -1;
        public int status = 0;
    }

    public partial class GameMatchManager 
    {
        protected int DBAIPlayerCount()
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT " +
	                $" COUNT(uid) as count, MAX(uid) as max " +
                    $"FROM `t_aiplayers` " +
                    $"WHERE status > 0; ";
                var result_code = db?.Query(sql);
                if(result_code < 0) {
                    return -1;
                }
                
                int count = (int)(db?.ResultItems["count"]?.Number ?? 0);
                int max = (int)(db?.ResultItems["max"]?.Number ?? 0);
                return count;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 这将获取全部有效AI，如果IsAIPlayerDerived为False时可以用
        /// 其它情况不建议使用
        /// </summary>
        /// <returns></returns>
        protected int DBAIPlayerData(List<AIPlayerData> list)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                list.Clear();
                
                //
                List<DatabaseResultItemSet>? result_list = null;
                
                // 默认只取1000条，这是比较消耗性能的
                string sql = 
                    $"SELECT " +
	                $" id, tid, name, level, hol_value, items, status " +
                    $"FROM `t_aiplayers` " +
                    $"WHERE status >= 0 " +
                    $"LIMIT ?; ";
                var result_code = db?.QueryWithList(sql, out result_list, 1000);
                if(result_code < 0 || result_list == null) {
                    return -1;
                }
                
                foreach(var v in result_list)
                {
                    var item = v.To<AIPlayerData>();
                    if(item == null) { continue; }

                    //
                    AIPlayerManager.Instance.InitPlayerData(item);

                    //
                    list.Add(item);
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBAIPlayerDataAdd(AIPlayerData data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                
                // 默认只取1000条，这是比较消耗性能的
                string sql = 
                    $"INSERT INTO `t_aiplayers` " +
                    $"  (`id`, `tid`, `name`, `level`, `hol_value`, `items`, `gender`, `region`) " +
                    $"VALUES " +
                    $"  (?,?, ?,?,100, ?, ?,?);";
                var result_code = db?.Query(sql, 
                    data.ID, data.TID, data.Name, data.Level, 
                    data.Items, 
                    data.TemplateData?.Gender == UserGender.Male ? "male" : "female", 
                    data.TemplateData?.Region ?? "");
                if(result_code < 0) {
                    return -1;
                }
                
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 取匹配队列
        /// </summary>
        /// <param name="list"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected int DBQueues(List<GameMatchQueueItem> list, GameMatchType type = GameMatchType.Normal)
        {
            var db = DatabaseManager.Instance.New();
            try
            {

                string type_name = type == GameMatchType.Normal ? "normal" : "ranking";
                

                list.Clear();

                //
                List<DatabaseResultItemSet>? result_list = null;

                // 
                string sql = 
                    $"SELECT " +
	                $"  m.sn, m.id AS server_id, u.name, m.hol AS hol_value, " +
                    $"  m.flag, m.create_time, m.last_time, " +
                    $"  TIMESTAMPDIFF(SECOND, m.create_time, NOW()) AS wait_time, m.status " +
                    $"FROM `t_matches` AS m " +
                    $"RIGHT JOIN `t_user` AS u ON u.id = m.id " +
                    $"WHERE " + 
	                $"  m.type = 'normal' AND m.level = 0 " +
                    $"  -- AND m.create_time > (NOW() - INTERVAL 30 MINUTE) " +
                    $"  -- AND m.last_time > (NOW() - INTERVAL 5 SECOND)  -- 仅保留最近5秒内有更新的记录 " +
	                $"  AND m.flag = 'waiting' AND m.status > 0" +
                    $"ORDER BY m.create_time ASC  -- 按等待时间倒序排列 " +
                    $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out result_list);
                if(result_code < 0 || result_list == null) {
                    return -1;
                }
                
                
                foreach(var v in result_list)
                {
                    var item = v.To<GameMatchQueueItem>();
                    if(item == null) { continue; }
                    //  取等待中的玩家
                    if(item.status != 1 || item.create_time == null) { continue; }

                    //
                    list.Add(item);
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="auth_data"></param>
        /// <param name="id">匹配流水单号</param>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        protected int DBMatchStart(SessionAuthData auth_data, string id, GameMatchType type = GameMatchType.Normal, int level = 0)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                string type_name = type == GameMatchType.Normal ? "normal" : "ranking";
                
                // 
                string sql = 
                    $"SELECT " + 
                    $"  h.id AS server_id, h.value AS hol_value, " +
                    $"  u.client_id " +
                    $"FROM t_hol AS h " +
                    $"RIGHT JOIN t_user AS u ON h.id = u.id AND u.status > 0 " +
                    $"WHERE h.id = ?; ";
                var result_code = db?.Query(sql, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }
                
                int hol_value = (int)(db?.ResultItems["hol_value"]?.Number ?? 100);

                // 
                sql =
                    $"INSERT INTO `t_matches` " +
                    $"(`sn`, `id`, `hol`, `type`, `level`, `flag`) " +
                    $"VALUES " +
                    $"(?, ?, ?, ?, ?, 'waiting')";
                result_code = db?.Query(sql, id,
                        auth_data.id, hol_value, type_name, level);
                if(result_code < 0) {
                    return -1;
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="auth_data"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected int DBMatchCancel(SessionAuthData auth_data, string id)
        {
            id = id.Trim();

            var db = DatabaseManager.Instance.New();
            try
            {                
                // 
                string sql = 
                    $"SELECT " + 
                    $"  h.id AS server_id, h.value AS hol_value, " +
                    $"  u.client_id " +
                    $"FROM t_hol AS h " +
                    $"RIGHT JOIN t_user AS u ON h.id = u.id AND u.status > 0 " +
                    $"WHERE h.id = ?; ";
                var result_code = db?.Query(sql, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }
                
                int hol_value = (int)(db?.ResultItems["hol_value"]?.Number ?? 100);

                // 
                sql =
                    $"UPDATE `t_matches`" +
                    $"SET " +
                    $"  `last_time` = NOW(), `flag` = 'cancelled', `status` = 0 " +
                    $"WHERE `sn` = ? AND `id` = ?;";
                result_code = db?.Query(sql, id, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

    }
}