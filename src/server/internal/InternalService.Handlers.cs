using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;

namespace Server
{

    [System.Serializable]
    public class NGamePVPCompletedRequest
    {
    }

    [System.Serializable]
    public class NGamePVPCompletedResponse
    {
        [JsonPropertyName("code")]
        public int Code;
    }

    /// <summary>
    /// 
    /// </summary>

    public partial class InternalService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGamePVPCompleted(HttpContext context)
        {
            //
            var result = new NGamePVPCompletedResponse
            {
                Code = 0,
            };

            //
            await context.ResponseResult(result);
        }
    }
}