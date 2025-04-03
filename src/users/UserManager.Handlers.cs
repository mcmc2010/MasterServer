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

            // 解析 JSON
            var user = await context.Request.JsonBodyAsync<NAuthUserRequest>();
            _logger?.Log($"(User) Auth User:{user?.UID} SessionUID:{user?.SessionUID}");

            string uid = AMToolkits.Utility.Guid.GeneratorID12N();

            long time = AMToolkits.Utility.Utils.GetLongTimestamp();
            int rand = AMToolkits.Utility.Guid.GeneratorID6();
            string passphrase = AMToolkits.Utility.Guid.GeneratorID8();
            string token = $"{uid}_{time}_{passphrase}_{rand}";
            token = AMToolkits.Utility.Hash.SHA256String(token);
            string date_time = AMToolkits.Utility.Utils.DateTimeToString(DateTime.UtcNow, 
                    AMToolkits.Utility.Utils.DATETIME_FORMAT_LONG_STRING);

            //
            var result = new NAuthUserResponse {
                Code = 0,
                UID = user?.UID ?? "",
                ServerUID = uid,
                Passphrase = passphrase,
                Token = token,
                DateTime = date_time
            };

            //
            await context.ResponseResult(result);
        }
    }
}