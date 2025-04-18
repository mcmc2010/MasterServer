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

        protected int DBRoomCreateOne(int rid, int max = 2)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"INSERT INTO `t_rooms` " +
                    $"(`id`, `ids_0`, `ids_1`) " +
                    $"VALUES " +
                    $"(?, NULL, NULL); ";
                var result_code = db?.Query(sql, rid);
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

        protected int DBRoomsInit()
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  ids_0 = NULL, ids_1 = NULL " +
                    $"WHERE service_uid = 0 AND status > 0;";
                var result_code = db?.Query(sql);
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


        protected int DBRoomIdle(RoomData room)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT " +
	                $"  id as rid " +
                    $"FROM `t_rooms` " +
                    $"WHERE service_uid = 0 AND status > 0; ";
                var result_code = db?.Query(sql);
                if(result_code < 0) {
                    return -1;
                }
                
                int rid = (int)(db?.ResultItems["rid"]?.Number ?? -1);
                room.RID = rid;
                return 1;
            } catch (Exception e) {
                _logger?.LogError("(Room) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBRoomSet(RoomData room, string id_0, string id_1)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                if(room.RID <= 0)
                {
                    return -1;
                }

                // 
                string sql = 
                    $"UPDATE `t_rooms` " +
                    $"SET " +
	                $"  ids_0 = ?, ids_1 = ? " +
                    $"WHERE id = ? AND service_uid = 0 AND status > 0;";
                var result_code = db?.Query(sql, id_0, id_1, room.RID);
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
    }
}
