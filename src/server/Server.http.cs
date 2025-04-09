
using AMToolkits.Utility;
using Microsoft.AspNetCore.Http;



////
namespace Server
{
    [System.Serializable]
    public class SessionAuthData
    {
        public string id = "";
        public int result = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int CheckSecretKey(HttpContext context)
        {
            var headers = context.Request.Headers;
            if (!headers.TryGetValue("X-SecretKey", out var value))
            {
                return -1;
            }

            bool is_root_key = false;
            string key = value.ToString().Trim().ToUpper();
            if(key.StartsWith("ROOT"))
            {
                is_root_key = true;

                string[] values = key.Split(":");
                if(values.Length > 1) {
                    key = values[1];
                }
            }

            if(key.Length != 16 && key.Length != 32 && key.Length != 64)
            {
                return -2;
            }

            if(is_root_key && key != ServerConfigLoader.Config.SecretKey)
            {
                return -7;
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int CheckLoginSession(HttpContext context, SessionAuthData? auth_data = null)
        {
            if(auth_data != null)
            {
                auth_data.result = -1;
            }

            var headers = context.Request.Headers;
            if (!headers.TryGetValue("X-Authorization", out var value))
            {
                return -1;
            }
            string text = value.ToString().Trim();

            string key = "";
            string token = "";
            string hash = "";
            string[] values = text.Split(":");
            if(values.Length > 0) {
                key = values[0].Trim();
            }
            if(values.Length > 1) {
                token = values[1].Trim();
            }
            if(values.Length > 2) {
                hash = values[2].Trim();
            }
            
            if(auth_data != null)
            {
                auth_data.id = key;
                auth_data.result = 0;
            }

            // 登陆验证
            if(_config?.JWTEnabled == true && JWTAuth.JWTVerifyData(hash, _config?.JWTSecretKey ?? "") <= 0)
            {
                return 0;
            }

            // DB 验证
            if(DBCheckLoginSession(key, token) <= 0)
            {
                return 0;
            }

            if(auth_data != null)
            {
                auth_data.id = key;
                auth_data.result = 1;
            }
            return 1;
        }
    }
}