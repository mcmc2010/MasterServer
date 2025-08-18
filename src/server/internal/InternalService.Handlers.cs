using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;

namespace Server
{

    [System.Serializable]
    public class NGamePVPPlayerData
    {
        [JsonPropertyName("ai")]
        public int AIPlayerIndex = 0; // AI玩家索引
        [JsonPropertyName("id")]
        public string UserID = "";
        [JsonPropertyName("name")]
        public string Name = "";
        // [JsonPropertyName("head")]
        // public string Head = "";

        [JsonPropertyName("is_victory")]
        public bool IsVictory = false;
    }


    [System.Serializable]
    public class NGamePVPCompletedRequest
    {
        [JsonPropertyName("roomType")]
        public int RoomType = 0;
        [JsonPropertyName("roomLv")]
        public int RoomLevel = 0;

        [JsonPropertyName("createTime")]
        public DateTime? CreateTime = null;
        [JsonPropertyName("endTime")]
        public DateTime? EndTime = null;

        [JsonPropertyName("winner")]
        public NGamePVPPlayerData? WinnerPlayer = null;
        [JsonPropertyName("loser")]
        public NGamePVPPlayerData? LoserPlayer = null;
    }

    [System.Serializable]
    public class NGamePVPCompletedResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
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

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGamePVPCompletedRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            string uid = AMToolkits.Utility.Guid.GeneratorID18N();

            //
            var result = new NGamePVPCompletedResponse
            {
                Code = 0,
                ID = uid
            };

            // 流水单
            var result_code = await this._UpdateGamePVPRecord(uid, request.RoomType, request.RoomLevel,
                                request.CreateTime, request.EndTime,
                                request.WinnerPlayer, request.LoserPlayer);
            if (result_code > 0)
            {
            }

            //
            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }
    }
}