
using System.Net;

using AMToolkits.Extensions;
using AMToolkits.Utility;
using Microsoft.AspNetCore.Http;
using Logger;


////
namespace Server
{
    [System.Serializable]
    public class SessionAuthData
    {
        public string id = "";
        public int result = 0;

        public string token = "";
        public string passphrase = "";
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
            if (key.StartsWith("ROOT"))
            {
                is_root_key = true;

                string[] values = key.Split(":");
                if (values.Length > 1)
                {
                    key = values[1];
                }
            }

            if (key.Length != 16 && key.Length != 32 && key.Length != 64)
            {
                return -2;
            }

            if (is_root_key && key != ServerConfigLoader.Config.SecretKey)
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
            if (auth_data == null)
            {
                auth_data = new SessionAuthData()
                {
                    result = -1
                };
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
            if (values.Length > 0)
            {
                key = values[0].Trim();
            }
            if (values.Length > 1)
            {
                token = values[1].Trim();
            }
            if (values.Length > 2)
            {
                hash = values[2].Trim();
            }

            auth_data.result = 0;

            // 登陆验证
            int result_code = 0;
            if (_config?.JWTEnabled == true && _config?.JWTAuthorizationEnabled == true
                && (result_code = JWTAuth.JWTVerifyData(hash, _config?.JWTSecretKey ?? "")) <= 0)
            {
                auth_data.result = result_code;
                return 0;
            }

            // DB 验证
            if (_config?.DBAuthorizationEnabled == true
                && (result_code = DBAuthenticationSession(key, token)) <= 0)
            {
                auth_data.result = result_code;
                return 0;
            }

            auth_data.id = key;
            auth_data.result = 1;
            var user = UserManager.Instance.GetUserT<UserBase>(key);
            if (user != null)
            {
                auth_data.token = user.AccessToken;
                auth_data.passphrase = user.Passphrase;
            }

            if (user?.AccessToken.ToUpper() != token.ToUpper())
            {
                _logger?.LogError($"(User) Auth User:{user?.ID}, Token:{user?.AccessToken} - {token} Error");
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="auth_data"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> AuthSessionAndResult(HttpContext context, SessionAuthData? auth_data = null)
        {
            int result = 0;
            if ((result = ServerApplication.Instance.CheckSecretKey(context)) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return result;
            }

            if ((result = ServerApplication.Instance.CheckLoginSession(context, auth_data)) <= 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotLogin);
                return result;
            }
            return result;
        }
    }
}
