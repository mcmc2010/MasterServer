using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public enum GameMatchType
    {
        Normal,
        Ranking,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum GameMatchState
    {
        None,
        Start,
        Waiting,
        Cancel,
        Error,
        Completed,
    }


    [System.Serializable]
    public class NGameMatchStartRequest
    {
        [JsonPropertyName("type")]
        public GameMatchType Type = GameMatchType.Normal;
        [JsonPropertyName("level")]
        public int Level = 0;
    }

    [System.Serializable]
    public class NGameMatchStartResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
    }

    public partial class GameMatchManager 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleStartMatch(HttpContext context)
        {
            if(ServerApplication.Instance.CheckSecretKey(context) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return;
            }
            
            SessionAuthData auth_data = new SessionAuthData();
            if(ServerApplication.Instance.CheckLoginSession(context, auth_data) <= 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotLogin);
                return;
            }

            // 解析 JSON
            var match = await context.Request.JsonBodyAsync<NGameMatchStartRequest>();
            if(match == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            string id = AMToolkits.Utility.Guid.GeneratorID18N();
            int result_code = this.DBMatchStart(auth_data, id, match.Type, match.Level);
                        
            //
            var result = new NGameMatchStartResponse {
                Code = result_code,
                ID = id,
            };

            //
            await context.ResponseResult(result);
        }
    }
}