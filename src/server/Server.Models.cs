using Logger;

namespace Server
{
    public partial class ServerApplication
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private int DBAuthenticationSession(string id, string token)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                // 
                string sql = 
                    $"SELECT uid, id AS server_id, client_id, token, passphrase, last_time, status " +
                    $"FROM t_user WHERE id = ? AND status >= 0;";
                var result_code = db?.Query(sql, id);
                if(result_code < 0) {
                    return -1;
                }
                // 账号不存在
                if(result_code == 0) {
                    return 0;
                }

                string access_token = db?.ResultItems["token"]?.String ?? "";
                int status = (int)(db?.ResultItems["status"]?.Number ?? 1);
                DateTime? last_time = db?.ResultItems["last_time"]?.Date;

                // 该账号不允许访问，已封禁
                if(status == 0)
                {
                    return -7;
                }

                // Token 不相同
                if(access_token.ToUpper() != token.ToUpper())
                {
                    return 0;
                }

                // 验证已经过时
                int expired = _config?.DBAuthorizationExpired ?? 1 * 24 * 60 * 60;
                if(last_time == null || 
                    ((System.TimeSpan)(DateTime.Now - last_time)).Seconds >= expired)
                {
                    return -6;
                }

                // 最后更新验证
                sql = 
                    $"UPDATE t_user SET last_time = NOW() " +
                    $"WHERE id = ? AND status > 0;";
                result_code = db?.Query(sql, id);
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