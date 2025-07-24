using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;


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
        [JsonPropertyName("privilege_level")]
        public int PrivilegeLevel = 0;
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
            var request = await context.Request.JsonBodyAsync<NAuthUserRequest>();

            string uid = AMToolkits.Utility.Guid.GeneratorID12N();

            long time = AMToolkits.Utils.GetLongTimestamp();
            int rand = AMToolkits.Utility.Guid.GeneratorID6();
            string passphrase = AMToolkits.Utility.Guid.GeneratorID8();
            string token = $"{uid}_{time}_{passphrase}_{rand}";
            token = AMToolkits.Hash.SHA256String(token);
            string date_time = AMToolkits.Utils.DateTimeToString(DateTime.UtcNow, 
                    AMToolkits.Utils.DATETIME_FORMAT_LONG_STRING);

            // 0: 第三方验证平台
            // PlayFab验证
            int result_code = await PlayFabService.Instance.PFUserAuthentication(request?.UID ?? "",
                    request?.SessionUID ?? "",
                    request?.SessionToken ?? "");
            if (result_code < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotLogin);
                return;
            }

            // 1: 验证用户
            var user_data = new UserAuthenticationData()
            {
                server_uid = uid,
                client_uid = request?.UID ?? "",
                custom_id = request?.SessionUID ?? "",
                passphrase = passphrase,
                token = token,
                datetime = DateTime.Now,
                device = $"{platform}",

                //
                jwt_token = ""
            };

            result_code = await this.AuthenticationAndInitUser(user_data);
            

            //
            var result = new NAuthUserResponse
            {
                Code = result_code,
                UID = user_data.client_uid,
                ServerUID = user_data.server_uid,
                Passphrase = user_data.passphrase,
                Token = user_data.token,
                DateTime = date_time,
                Hash = user_data.jwt_token,
                PrivilegeLevel = user_data.privilege_level,
            };

            //
            await context.ResponseResult(result);
        }
    }
}