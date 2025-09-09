using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;
using AMToolkits.Game;


namespace Server
{
    #region User

    /// <summary>
    /// 获取钱包
    /// Endpoint: api/internal/user/wallet/data
    /// </summary>
    [System.Serializable]
    public class NUserWalletDataRequest
    {
        /// <summary>
        /// 玩家账号
        /// </summary>
        [JsonPropertyName("user_uid")]
        public string UserID = "";

        /// <summary>
        /// 目前是PlayfabID
        /// </summary>
        [JsonPropertyName("custom_uid")]
        public string CustomID = "";
    }

    [System.Serializable]
    public class NUserWalletDataResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public WalletData? Data = null;
    }

    /// <summary>
    /// 更新虚拟货币
    /// Endpoint: api/internal/user/wallet/update
    /// </summary>
    [System.Serializable]
    public class NUserWalletUpdateRequest
    {
        /// <summary>
        /// 玩家账号
        /// </summary>
        [JsonPropertyName("user_uid")]
        public string UserID = "";

        /// <summary>
        /// 目前是PlayfabID
        /// </summary>
        [JsonPropertyName("custom_uid")]
        public string CustomID = "";

        [JsonPropertyName("amount")]
        public float Amount = 0.0f;

        /// <summary>
        /// 0:金币，1:钻石
        /// </summary>
        [JsonPropertyName("currency")]
        public int VirtualCurrency = (int)AMToolkits.Game.VirtualCurrency.GD;
    }

    [System.Serializable]
    public class NUserWalletUpdateResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public Dictionary<string, object?>? Data = null;
    }

    #endregion

    #region Game PVP
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

    /// <summary>
    ///  {
    ///     "roomType":1,
    ///     "roomLv":0,
    ///     "createTime":1756204275,
    ///     "endTime":1756204293,
    ///     "loser":{
    ///         "ai":0,
    ///         "id":"155592223876",
    ///         "name":"155592223876",
    ///         "head":"Player/Icons/1_01",
    ///         "ballsPocketed":0,
    ///         "foulsCommitted":0
    ///     }
    ///  }
    /// </summary>
    [System.Serializable]
    public class NGamePVPCompletedRequest
    {
        [JsonPropertyName("roomType")]
        public int RoomType = 0;
        [JsonPropertyName("roomLv")]
        public int RoomLevel = 0;

        [JsonPropertyName("createTime")]
        public object? create_time = null;

        [JsonPropertyName("endTime")]
        public object? end_time = null;
        public DateTime? AsCreateTime => create_time.AsDateTime();
        public DateTime? AsEndTime => end_time.AsDateTime();

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

    #endregion

    /// <summary>
    /// 
    /// </summary>

    public partial class InternalService
    {
        /// <summary>
        /// 获取钱包数据
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserWalletData(HttpContext context)
        {
            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUserWalletDataRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }


            //
            var result = new NUserWalletDataResponse
            {
                Code = 0,
            };

            
            // 
            var result_data = await this._GetWalletData(request.UserID, request.CustomID);
            if (result_data == null) {
                result.Code = -1;
            }
            else
            {
                //
                result.Code = 1;
                result.Data = result_data;
            }

            //
            await context.ResponseResult(result);
        }

        /// <summary>
        /// 更新钱包
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserWalletUpdate(HttpContext context)
        {
            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUserWalletUpdateRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }


            //
            var result = new NUserWalletUpdateResponse
            {
                Code = 0,
            };


            // 
            var result_data = await this._UpdateVirtualCurrency(request.UserID, request.CustomID, request.Amount,
                                (AMToolkits.Game.VirtualCurrency)request.VirtualCurrency);
            if (result_data == null)
            {
                result.Code = -1;
            }
            else
            {
                //
                result.Code = 1;
                result.Data = result_data;
            }

            //
            await context.ResponseResult(result);
        }


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
                                request.AsCreateTime, request.AsEndTime,
                                request.WinnerPlayer, request.LoserPlayer);
            if (result_code < 0)
            {
                _logger?.LogError($"{TAGName} (GamePVPCompleted) : ({uid}) Room {request.RoomType} - {request.RoomLevel}," +
                                  $"{request.WinnerPlayer?.UserID} - {request.WinnerPlayer?.Name} vs {request.LoserPlayer?.UserID} - {request.LoserPlayer?.Name}" +
                                  $", Result: {result_code}");
            }
            else
            {
                _logger?.Log($"{TAGName} (GamePVPCompleted) : ({uid}) Room {request.RoomType} - {request.RoomLevel}," +
                             $"{request.WinnerPlayer?.UserID} - {request.WinnerPlayer?.Name} vs {request.LoserPlayer?.UserID} - {request.LoserPlayer?.Name}" +
                             $"");
            }

            //
            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }



    }
}