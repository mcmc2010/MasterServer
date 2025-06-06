using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;
using AMToolkits.Utility;

namespace Server
{
    [System.Serializable]
    public class NAuthUserRequest
    {
        [JsonPropertyName("uid")]
        public string UID = "";
        [JsonPropertyName("type")]
        public string Type = "NONE";
        [JsonPropertyName("session_uid")]
        public string SessionUID = "";
        [JsonPropertyName("session_token")]
        public string SessionToken = "";
    }

    [System.Serializable]
    public class NAuthUserResponse
    {
        [JsonPropertyName("code")]
        public int Code = 0;
        [JsonPropertyName("uid")]
        public string UID = "";
        [JsonPropertyName("server_uid")]
        public string ServerUID = "";
        [JsonPropertyName("passphrase")]
        public string Passphrase = "";
        [JsonPropertyName("token")]
        public string Token = "";
        [JsonPropertyName("time")]
        public string DateTime = "";
        [JsonPropertyName("hash")]
        public string Hash = "";
    }

    public partial class UserManager
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserAuth(HttpContext context)
        {
            if(ServerApplication.Instance.CheckSecretKey(context) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return;
            }

            //
            var platform = "";
            context.GetOSPlatform(out platform);

            // 解析 JSON
            var user = await context.Request.JsonBodyAsync<NAuthUserRequest>();

            string uid = AMToolkits.Utility.Guid.GeneratorID12N();

            long time = AMToolkits.Utility.Utils.GetLongTimestamp();
            int rand = AMToolkits.Utility.Guid.GeneratorID6();
            string passphrase = AMToolkits.Utility.Guid.GeneratorID8();
            string token = $"{uid}_{time}_{passphrase}_{rand}";
            token = AMToolkits.Utility.Hash.SHA256String(token);
            string date_time = AMToolkits.Utility.Utils.DateTimeToString(DateTime.UtcNow, 
                    AMToolkits.Utility.Utils.DATETIME_FORMAT_LONG_STRING);

            // 0: 第三方验证平台
            // 1: DB验证用户
            var user_data = new DBAuthUserData()
            {
                server_uid = uid,
                client_uid = user?.UID ?? "",
                custom_id = user?.SessionUID ?? "",
                passphrase = passphrase,
                token = token,
                datetime = DateTime.Now,
                device = $"{platform}"
            };

            int result_code = this.DBAuthUser(user_data);
            if(result_code < 0) {
                user_data.passphrase = "";
                user_data.token = "";
            }

            // 2: HOL
            if(result_code > 0) {
                result_code = this.DBInitHOL(user_data);
            }
            
            // 3: 
            string hash = "";
            if(result_code > 0) {

                // JWT
                if(_config?.JWTEnabled == true) {
                    hash = JWTAuth.JWTSignData(new Dictionary<string, object>() {
                        { "uid", user_data.client_uid },
                        { "server_uid", user_data.server_uid },
                        { "token", user_data.token }
                    }, _config?.JWTSecretKey ?? "", _config?.JWTExpired ?? -1);
                    // JWT 认证失败
                    if(hash.Length == 0)
                    {
                        //result_code = -100;
                    }
                }

                // Add User To Manager
                this.AddUser(new UserBase() {
                    ID = user_data.server_uid,
                    ClientID = user_data.client_uid,
                    AccessToken = user_data.token,
                    Passphrase = user_data.passphrase
                });
            }

            //
            _logger?.Log($"(User) Auth User:{user_data.client_uid} - {user_data.server_uid}, Token:{user_data.token} Result: {result_code}");

            //
            var result = new NAuthUserResponse {
                Code = result_code,
                UID = user_data.client_uid,
                ServerUID = user_data.server_uid,
                Passphrase = user_data.passphrase,
                Token = user_data.token,
                DateTime = date_time,
                Hash = hash,
            };

            //
            await context.ResponseResult(result);
        }
    }
}