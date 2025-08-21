
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;


namespace Server
{

    [System.Serializable]
    public class NGameEventFinalRequest
    {
        [JsonPropertyName("id")]
        public int ID = -1;
    }

    [System.Serializable]
    public class NGameEventFinalResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("id")]
        public int ID = -1; 
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class GameEventsManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGameEventFinal(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGameEventFinalRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NGameEventFinalResponse
            {
                Code = 0,
                ID = request.ID,
            };

            //
            var b_result = await this.GameEventFinal(auth_data.id, request.ID);
            if (b_result.Code > 0)
            {
                if (b_result.Items != null)
                {
                    //result.Items = b_result.Items.Select(v => v).ToList<object?>();
                }
            }

            
            result.Code = b_result.Code;

            //
            await context.ResponseResult(result);
        }
    }
}