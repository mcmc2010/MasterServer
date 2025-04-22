
using Logger;



namespace Server
{
    public partial class RoomManager 
    {
        protected int DBRoomCount()
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT " +
	                $" COUNT(uid) as count, MAX(uid) as max " +
                    $"FROM `t_rooms` " +
                    $"WHERE status >= 0; ";
                var result_code = db?.Query(sql);
                if(result_code < 0) {
                    return -1;
                }
                
                int count = (int)(db?.ResultItems["count"]?.Number ?? 0);
                int max = (int)(db?.ResultItems["max"]?.Number ?? 0);
                return count;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 数据库房间数量不够时创建，仅仅用于服务初始化阶段
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="max">玩家最大人数</param>
        /// <returns></returns>
        protected int DBRoomCreate(int rid, int max = 2, int service_id = 0)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"INSERT INTO `t_rooms` " +
                    $"(`id`, `name`, `creator_id`, `cur_num`, `max_num`, `secret_key`, `service_id`) " +
                    $"VALUES " +
                    $"(?, NULL, NULL, 0, 0, NULL, ?); ";
                var result_code = db?.Query(sql, rid, service_id);
                if(result_code < 0) {
                    return -1;
                }
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 与数据库中房间数据同步，这里需要排除其它服务使用和管理的房间
        /// </summary>
        /// <returns></returns>
        protected int DBRoomsInit(int max, int service_id = 0)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  creator_id = NULL, cur_num = 0, max_num = ?, " +
                    $"  create_time = NOW(), last_time = NOW() "+
                    $"WHERE service_id = ? AND status > 0;";
                var result_code = db?.Query(sql, max, service_id);
                if(result_code < 0) {
                    return -1;
                }
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }


        protected int DBGetIdleRoom(RoomData room)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT " +
	                $"  id as rid, name, creator_id, cur_num, max_num, r.create_time, r.last_time " +
                    $"  " +
                    $"FROM `t_rooms` AS r " +
                    $"WHERE " +
                    $"  (TIME_TO_SEC(TIMEDIFF(r.create_time, r.last_time)) < 3 OR r.last_time < (NOW() - INTERVAL 30 SECOND))  " +
                    $"  AND creator_id is NULL " + 
                    $"  AND service_id = ? AND status > 0 " +
                    //$"-- ORDER BY r.last_time ASC " +
                    $"ORDER BY RAND() " +
                    $"LIMIT 1;";
                var result_code = db?.Query(sql, room.ServiceID);
                if(result_code <= 0) {
                    return -1;
                }
                
                int rid = (int)(db?.ResultItems["rid"]?.Number ?? -1);

                int cur_num = 0; //(int)(db?.ResultItems["cur_num"]?.Number ?? 0);
                int max_num = (int)(db?.ResultItems["max_num"]?.Number ?? 0);

                string secret_key = AMToolkits.Utility.Hash.MD5String(
                    $"{AMToolkits.Utility.Utils.GetTimestamp()}_{AMToolkits.Utility.Guid.GeneratorID8()}");

                //每次使用房间密钥都不一样
                sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  cur_num = 0, secret_key = ?, last_time = NOW() " +
                    $"WHERE id = ? AND service_id = ? AND status > 0;";
                result_code = db?.Query(sql, 
                    secret_key,
                    rid, room.ServiceID);
                if(result_code < 0) {
                    return -1;
                }

                room.RID = rid;
                room.CurNum = cur_num;
                room.MaxNum = max_num;
                room.SecretKey = secret_key;
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBSetIdleRoom(RoomData room)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 0: 设置房间
                string sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  creator_id = NULL, cur_num = 0, last_time = NOW() " +
                    $"WHERE id = ? AND service_id = ? AND status > 0;";
                var result_code = db?.Query(sql, room.RID, room.ServiceID);
                if(result_code < 0) {
                    return -1;
                }

                // 1: 设置房间玩家
                sql = 
                    $"UPDATE `t_rooms_players` " +
                    $"SET " +
	                $"  leave_time = NOW(), status = 0 " +
                    $"WHERE rid = ? AND status > 0;";
                result_code = db?.Query(sql, room.RID);
                if(result_code < 0) {
                    return -1;
                }
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 设置房主
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        protected int DBSetMasterPlayerInRoom(RoomData room, RoomPlayerData creator)
        {
            if(room.RID <= 0)
            {
                return -1;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 0: 更新房间表
                string sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  creator_id = ?, cur_num = ? " +
                    $"WHERE id = ? AND service_id = ? AND status > 0;";
                var result_code = db?.Query(sql, creator.ID, 0,
                    room.RID, room.ServiceID);
                if(result_code < 0) {
                    db?.Rollback();
                    return -1;
                }

                // 1: 向房间玩家表中插入玩家数据
                sql = 
                    $"INSERT INTO `t_rooms_players` " +
                    $"  (`rid`,`id`,`joined_time`,`leave_time`,`role`) " +
	                $"VALUES " +
                    $"  (?,?, NULL,NULL, 'master');";
                result_code = db?.Query(sql, room.RID, creator.ID);
                if(result_code < 0) {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        protected int DBSetPlayerInRoom(RoomData room, RoomPlayerData player)
        {
            if(room.RID <= 0)
            {
                return -1;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 0: 向房间玩家表中插入玩家数据
                string sql = 
                    $"INSERT INTO `t_rooms_players` " +
                    $"  (`rid`,`id`,`joined_time`,`leave_time`,`role`) " +
	                $"VALUES " +
                    $"  (?,?, NULL,NULL, 'member');";
                var result_code = db?.Query(sql, room.RID, player.ID);
                if(result_code < 0) {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
    }
}
