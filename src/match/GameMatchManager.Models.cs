using System.Security.Policy;
using Logger;

namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class GameMatchQueueItem
    {
        public string sn = "";
        public string server_id = "";
        public string name = "";
        public int tid = 0;
        public int hol_value = 0;
        public GameMatchType type = GameMatchType.Normal;
        public GameMatchRoomRole role = GameMatchRoomRole.None;
        public GameMatchTeam team = GameMatchTeam.Blue;
        public int level = 0;
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
        /// 服务启动时对AI进行重置
        /// </summary>
        /// <returns></returns>
        protected int DBAIPlayerInit()
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 目前只有一个匹配状态需要重置
                string sql = 
                    $"UPDATE `t_aiplayers` SET " +
	                $" `match_status` = 0 " +
                    $"WHERE " +
                    $"  status > 0; ";
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
	                $" id, tid, name, level, hol AS hol_value, items, status " +
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
                    $"  (`id`, `tid`, `name`, `level`, `hol`, `items`, `gender`, `region`) " +
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
        /// 对队列里已经超时的纪录设置超时
        /// </summary>
        /// <returns></returns>
        protected int DBQueuesTimeout()
        {
            var db = DatabaseManager.Instance.New();
            try
            {

                // 0: 对大于30分钟的100条纪录等待队列设置超时
                string sql = 
                    $"UPDATE `t_matches` " +
                    $"SET  " +
                    $"  `match_status` = 'timeout', `status` = 0 " +
                    $"WHERE " +
                    $"  `create_time` < (NOW() - INTERVAL 30 MINUTE) AND " +
                    $"  `match_status` = 'waiting' AND status > 0 LIMIT 100;";
                var result_code = db?.Query(sql);
                if(result_code < 0) {
                    return -1;
                }

                // 1: 对大于1小时的100未完纪录成队列设置归档
                sql = 
                    $"UPDATE `t_matches` " +
                    $"SET  " +
                    $"  `status` = 0 " +
                    $"WHERE " +
                    $"  `create_time` < (NOW() - INTERVAL 60 MINUTE) AND " +
                    $"  status > 0 LIMIT 100;";
                result_code = db?.Query(sql);
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
                int level = 0;
                string type_name = type == GameMatchType.Normal ? "normal" : "ranking";
                

                list.Clear();

                //
                List<DatabaseResultItemSet>? result_list = null;

                // 
                string sql = 
                    $"SELECT " +
	                $"  m.sn, m.id AS server_id, u.name, m.hol AS hol_value, " +
                    $"  m.match_status, m.create_time, m.last_time, " +
                    $"  TIMESTAMPDIFF(SECOND, m.create_time, NOW()) AS wait_time, m.status " +
                    $"FROM `t_matches` AS m " +
                    $"RIGHT JOIN `t_user` AS u ON u.id = m.id " +
                    $"WHERE " + 
	                $"  m.type = ? AND m.level = ? AND m.tid = 0 " +
                    $"  AND m.create_time > (NOW() - INTERVAL 30 MINUTE) " +
                    //$"  -- AND m.last_time > (NOW() - INTERVAL 5 SECOND) " +
	                $"  AND m.match_status = 'waiting' AND m.status > 0 " +
                    $"ORDER BY m.create_time ASC " +
                    $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out result_list, 
                        type_name, level);
                if(result_code < 0 || result_list == null) {
                    return -1;
                }
                
                
                foreach(var v in result_list)
                {
                    var item = v.To<GameMatchQueueItem>();
                    if(item == null) { continue; }
                    //  取等待中的玩家
                    if(item.status != 1 || item.create_time == null) { continue; }

                    item.type = type;
                    item.level= level;
                    //
                    list.Add(item);
                }

                // 剔除重复ID，只保留时间最新
                list = list.GroupBy(v => v.server_id).
                                Select(g => g.OrderByDescending(v => v.create_time).First()).ToList();

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
                    $"(`sn`, `id`, `hol`, `type`, `level`, `match_status`) " +
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
                // 0:
                string sql = 
                    $"SELECT " + 
                    $"  h.id AS server_id, h.value AS hol_value, " +
                    $"  u.client_id, m.sn, m.room_id, m.match_code " +
                    $"FROM t_hol AS h " +
                    $"RIGHT JOIN t_user AS u ON h.id = u.id AND u.status > 0 " +
                    $"RIGHT JOIN t_matches AS m ON h.id = m.id AND m.sn = ? AND m.status > 0 " +
                    $"WHERE h.id = ?; ";
                var result_code = db?.Query(sql, id, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }
                
                int hol_value = (int)(db?.ResultItems["hol_value"]?.Number ?? 100);
                int rid = (int)(db?.ResultItems["room_id"]?.Number ?? -1);
                int mcode = (int)(db?.ResultItems["match_code"]?.Number ?? 0);

                // 1:
                sql =
                    $"UPDATE `t_matches`" +
                    $"SET " +
                    $"  `last_time` = NOW(), `match_status` = 'cancelled', `status` = 0 " +
                    $"WHERE `sn` = ? AND `id` = ?;";
                result_code = db?.Query(sql, id, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }

                // 1 - 1: 默认玩家是房主,房主取消
                // 关联AI也自动取消
                if(rid > 0) {
                    sql =
                        $"UPDATE `t_matches`" +
                        $"SET " +
                        $"  `last_time` = NOW(), `match_status` = 'cancelled', `status` = 0 " +
                        $"WHERE `room_id` = ? AND `id` <> ? AND `match_code` = ?;";
                    result_code = db?.Query(sql, rid, auth_data.id, mcode);
                    if(result_code < 0) {
                        return -1;
                    }
                }

                //2:
                if(rid > 0)
                {
                    RoomManager.Instance.SetIdleRoom(rid);
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBMatchCompleted(SessionAuthData auth_data, string id, out GameMatchQueueItem? item)
        {
            item = null;

            id = id.Trim();

            var db = DatabaseManager.Instance.New();
            try
            {                
                // 0:
                string sql = 
                    $"SELECT " + 
                    $"  h.id AS server_id, h.value AS hol_value, " +
                    $"  u.client_id, m.sn, m.room_id, m.match_code, m.match_status " +
                    $"FROM t_hol AS h " +
                    $"RIGHT JOIN t_user AS u ON h.id = u.id AND u.status > 0 " +
                    $"RIGHT JOIN t_matches AS m ON h.id = m.id AND m.sn = ? AND m.status > 0 " +
                    $"WHERE h.id = ?; ";
                var result_code = db?.Query(sql, id, auth_data.id);
                if(result_code < 0) {
                    return -1;
                }
                
                int rid = (int)(db?.ResultItems["room_id"]?.Number ?? -1);
                int mcode = (int)(db?.ResultItems["match_code"]?.Number ?? 0);
                if(rid <= 0 || mcode == 0) {
                    return 0;
                }

                // 1:
                sql =
                    $"SELECT " +
                    $"  sn, m.id AS server_id, m.room_id, m.match_code, m.match_status, " +
                    $"  a.tid, a.name, a.level, a.hol AS hol_value, " +
                    $"  rp.role  " +
                    $"FROM `t_matches` AS m " +
                    $"RIGHT JOIN `t_aiplayers` AS a ON m.id = a.id AND a.status > 0 " +
                    $"RIGHT JOIN `t_rooms_players` AS rp ON m.room_id = rp.rid AND m.id = rp.id AND rp.status > 0  " +
                    $"WHERE " +
                    $"  m.room_id = ? AND m.id <> ? AND m.match_code = ? AND m.status > 0 " +
                    $"ORDER BY m.create_time DESC LIMIT 1;";
                result_code = db?.Query(sql, rid, auth_data.id, mcode);
                if(result_code < 0) {
                    return -1;
                }

                var result = new Dictionary<string, DatabaseResultItem>(db.ResultItems);
                
                item = this.NewQueueItem(id, result, GameMatchType.Normal, GameMatchTeam.Red);
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 内部调用，不能直接使用
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id">流水单ID</param>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        protected int DBMatchStartWithAI(DatabaseQuery query, IDictionary<string, DatabaseResultItem> data, 
                string id, GameMatchType type = GameMatchType.Normal, int level = 0)
        {

            string type_name = type == GameMatchType.Normal ? "normal" : "ranking";

            //
            string server_id = data["server_id"]?.String ?? "";
            string name = data["name"]?.String ?? "";
            int tid = (int)(data["tid"]?.Number ?? 100);
            int hol_value = (int)(data["hol_value"]?.Number ?? 100);
            int total_matched = (int)(data["total_matched"]?.Number ?? 0);
            if(string.IsNullOrEmpty(server_id))
            {
                return -1;
            }

            // 
            string sql =
                $"INSERT INTO `t_matches` " +
                $"(`sn`, `id`, `tid`, `name`, `hol`, `type`, `level`, `match_status`) " +
                $"VALUES " +
                $"(?, ?,?,?,?, ?,?,'waiting'); ";
            int result_code = query.Query(sql, id,
                    server_id, tid, name, hol_value, type_name, level);
            if(result_code < 0) {
                return -1;
            }

            sql = 
                $"UPDATE `t_aiplayers` " +
                $"SET " +
                $"  `last_time` = NOW(), `match_status` = 1, `total_matched` = ? " +
                $"WHERE `id` = ?; ";
            result_code = query.Query(sql,
                    total_matched ++, server_id);
            if(result_code < 0) {
                return -1;
            }
            return 1;
        }

        /// <summary>
        /// 匹配玩家和对手玩家（AI）
        /// </summary>
        /// <param name="query"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected int DBMatchPairsWithAI(DatabaseQuery? query, 
                GameMatchQueueItem player, out GameMatchQueueItem? opponent)
        {
            opponent = null;
            if(query == null) {
                return -1;
            }

            // 0: 随机获取一个AI玩家
            string sql = 
                $"SELECT " + 
                $"  id AS server_id, tid, name, level, hol AS hol_value, match_status, " +
                $"  total_matched, total_played, last_time " + 
                $"FROM `t_aiplayers` " +
                $"WHERE " + 
	            $"  match_status = 0 AND status > 0 " +
                $"ORDER BY RAND() LIMIT 1;";
            var result_code = query.Query(sql);
            if(result_code < 0) {
                return -1;
            }

            var result = new Dictionary<string, DatabaseResultItem>(query.ResultItems);
            if(result.Count == 0) {
                return 0;
            }

            string server_id = result["server_id"]?.String ?? "";
            int tid = (int)(result["tid"]?.Number ?? 100);
            int level = (int)(result["level"]?.Number ?? 0);

            // 1: 直接插入等待队列
            string sn = AMToolkits.Utility.Guid.GeneratorID18N();
            result_code = DBMatchStartWithAI(query, result, sn, player.type, level);
            if(result_code < 0) {
                return -1;
            }

            // 2: 将一个小时内所有关联的纪录更新为超时
            sql = 
                $"UPDATE `t_matches` " +
                $"SET " +
                $"  `match_status` = 'timeout', `status` = 0 " +
                $"WHERE " +
	            $"   id = ? AND match_status = 'waiting' AND sn <> ? " +
	            $"   AND create_time > (NOW() - INTERVAL 60 MINUTE); ";
            result_code = query.Query(sql, server_id, sn);
            if(result_code < 0) {
                return -1;
            }

            opponent = NewQueueItem(sn, result, player.type, 
                    player.team == GameMatchTeam.Blue ? GameMatchTeam.Red : GameMatchTeam.Blue);
            return 1;
        }

        protected int DBMatchPlayerJoinRoom(DatabaseQuery? query, int match_code, RoomData room, GameMatchQueueItem item)
        {
            if(query == null) {
                return -1;
            }

            // 0: 
            string sql = 
                $"UPDATE `t_matches` " +
                $"SET " +
                $"  `match_status` = 'matched', `match_code` = ?, `room_id` = ?  " +
                $"WHERE " +
	            $"   sn = ? AND match_status = 'waiting' AND status > 0;";
            int result_code = query.Query(sql, match_code, room.RID,  item.sn);
            if(result_code < 0) {
                return -1;
            }
            return 1;
        }

        protected async Task<int> DBMatchWithAIProcess(List<GameMatchQueueItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {   
                int count = 0;
                foreach(var item in items)
                {
                    db?.Transaction();

                    item.role = GameMatchRoomRole.Master;

                    GameMatchQueueItem? opponent;
                    // 对应纪录匹配一条AI纪录
                    int result = this.DBMatchPairsWithAI(db, item, out opponent);
                    if(result < 0 || opponent == null) {
                        db?.Rollback();
                        continue;
                    }
                    opponent.role = GameMatchRoomRole.Member;

                    // 获取一个空置中的房间
                    var room = RoomManager.Instance.GetIdleRoom();
                    if(room == null)
                    {
                        db?.Commit();
                        break;
                    }

                    // 设置房间
                    int code = _rand.Next(100000, 999999);
                    room.RCODE = code;

                    result = this.DBMatchPlayerJoinRoom(db, code, room, item);
                    if(result < 0) {
                        db?.Rollback();
                        continue;
                    }
                    result = this.DBMatchPlayerJoinRoom(db, code, room, opponent);
                    if(result < 0) {
                        db?.Rollback();
                        continue;
                    }

                    // 关联房间
                    if(RoomManager.Instance.SetPlayersInRoomWithMatch(room, item, opponent) <= 0)
                    {
                        db?.Rollback();
                        continue;
                    }

                    db?.Commit();

                    // 没有可匹配的玩家
                    if(result == 0) {
                        break;
                    }

                    _logger?.Log($"{TAGName} (Queue) : ({item.sn}:{item.server_id}) - ({item.sn}:{opponent.server_id})");
                    count ++;
                }          

                return count;
            } catch (Exception e) {
                _logger?.LogError("(Match) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
    }
}