using Logger;

namespace Server
{
    public partial class GameMatchManager 
    {
        protected int DBQueues(GameMatchType type = GameMatchType.Normal)
        {
            var db = DatabaseManager.Instance.New();
            try
            {

                string type_name = type == GameMatchType.Normal ? "normal" : "ranking";
                

                List<DatabaseResultItemSet>? list = null;

                // 
                string sql = 
                    $"SELECT " +
	                $"  m.sn, m.id AS server_id, u.name, m.hol AS hol_value, " +
                    $"  m.flag, m.create_time, m.last_time, " +
                    $"  TIMESTAMPDIFF(SECOND, m.create_time, NOW()) AS wait_time " +
                    $"FROM `t_matches` AS m " +
                    $"RIGHT JOIN `t_user` AS u ON u.id = m.id " +
                    $"WHERE " + 
	                $"  m.type = 'normal' AND m.level = 0 " +
                    $"  -- AND m.create_time > (NOW() - INTERVAL 30 MINUTE) " +
                    $"  -- AND m.last_time > (NOW() - INTERVAL 5 SECOND)  -- 仅保留最近5秒内有更新的记录 " +
	                $"  AND m.flag = 'waiting' AND m.status > 0" +
                    $"ORDER BY m.create_time ASC  -- 按等待时间倒序排列 " +
                    $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out list);
                if(result_code < 0 || list == null) {
                    return -1;
                }
                
                _logger?.Log($"{TAGName} (Queue) Count:{list.Count}");

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