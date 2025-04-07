
using Logger;

//
namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class DBUserData
    {
        public string server_uid = "";
        public string client_uid = "";
        public string passphrase = "";
        public string token = "";
        public DateTime datetime = DateTime.Now;
    }

    /// <summary>
    /// 
    /// </summary>
    public class DBAuthUserData : DBUserData
    {
        ///
        public string custom_id = "";
    }

    public partial class UserManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_data"></param>
        /// <returns></returns>
        protected int DBAuthUser(DBAuthUserData user_data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // PlayFab 
                string sql = 
                    $"SELECT uid, id AS server_id, client_id, token, passphrase, last_time, status " +
                    $"FROM t_user WHERE client_id = ? AND playfab_id = ? AND status >= 0;";
                var result_code = db?.Query(sql, user_data.client_uid, user_data.custom_id);
                if(result_code < 0) {
                    return -1;
                }
                // 不存在
                // 自动创建新纪录
                else if(result_code == 0) {
                    sql =
                        $"INSERT INTO `t_user` " +
                        $"(`id`,`client_id`," +
                        $"`token`,`passphrase`,`playfab_id`)" +
                        $"VALUES(?, ?,  ?,?,  ?);";
                    result_code = db?.Query(sql, 
                        user_data.server_uid, user_data.client_uid, 
                        user_data.token, user_data.passphrase,
                        user_data.custom_id);
                    if(result_code < 0) {
                        return -1;
                    }
                }
                // 更新
                else
                {
                    int uid = (int)(db?.GetResultItem("uid")?.Number ?? -1);
                    int status = (int)(db?.GetResultItem("status")?.Number ?? 1);

                    user_data.server_uid = db?.GetResultItem("server_id")?.String ?? "";
                    if(user_data.server_uid.Length == 0)
                    {
                        return -1;
                    }

                    // 该账号不允许访问，已封禁
                    if(status == 0)
                    {
                        return -7;
                    }

                    sql = 
                        $"UPDATE `t_user` " +
                        $"SET " + 
                        $"    `token` = ?, `passphrase` = ?, " +
                        $"    `last_time` = NOW() " +
                        $"WHERE `id` = ? AND `uid` = ?;";
                    result_code = db?.Query(sql, 
                        user_data.token, user_data.passphrase,
                        user_data.server_uid, uid);

                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(User) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }

        protected int DBInitHOL(DBUserData user_data)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT uid, id AS server_id, value, last_time, status " +
                    $"FROM t_hol WHERE id = ? AND status >= 0;";
                var result_code = db?.Query(sql, user_data.server_uid);
                if(result_code < 0) {
                    return -1;
                }

                // 不存在
                // 自动创建新纪录
                else if(result_code == 0) {
                    sql =
                        $"INSERT INTO `t_hol` " +
                        $"(`id`,`value`)" +
                        $"VALUES(?, ?);";
                    result_code = db?.Query(sql, 
                        user_data.server_uid, 100);
                    if(result_code < 0) {
                        return -1;
                    }
                }

                return 1;
            } catch (Exception e) {
                _logger?.LogError("(User) Error :" + e.Message);
            } finally {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
    }
}