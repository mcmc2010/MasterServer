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
        Error = -1,
        None  = 0,
        Start,
        Waiting,
        Matched,
        Timeout,
        Cancelled,
        Game,
        Completed,
    }


    /// <summary>
    /// 
    /// </summary>
    public enum GameMatchPlayerType
    {
        Self = 0x00, //自己
        Opponent = 0x01, //玩家
        AI = 0x10
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


    [System.Serializable]
    public class NGameMatchCancelRequest
    {
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
    }
    [System.Serializable]
    public class NGameMatchCancelResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
    }


    [System.Serializable]
    public class NGameMatchCompletedRequest
    {
        [JsonPropertyName("index")]
        public int Index = 0; 
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
    }
    [System.Serializable]
    public class NGameMatchCompletedResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("index")]
        public int Index = 0; 
        // 匹配到玩家的类型，目前只有AI
        [JsonPropertyName("player_type")]
        public GameMatchPlayerType PlayerType = GameMatchPlayerType.AI;
        [JsonPropertyName("player_id")]
        public string PlayerID = "";
        // 玩家模版ID
        [JsonPropertyName("player_tid")]
        public int PlayerTID = 0;
        [JsonPropertyName("player_name")]
        public string PlayerName = "";
    }

    public partial class GameMatchManager 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleMatchStart(HttpContext context)
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
            if(result_code > 0)
            {
                if(_config?.MatchServer.IsAIPlayerEnabled == true) {
                    // 增加AI玩家
                    this.AddAIPlayer(match.Level);
                }
            }   

            //
            var result = new NGameMatchStartResponse {
                Code = result_code,
                ID = id,
            };

            //
            await context.ResponseResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleMatchCancel(HttpContext context)
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
            var match = await context.Request.JsonBodyAsync<NGameMatchCancelRequest>();
            if(match == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            int result_code = this.DBMatchCancel(auth_data, match.ID);
                        
            //
            var result = new NGameMatchCancelResponse {
                Code = result_code,
                ID = match.ID,
            };

            //
            await context.ResponseResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleMatchCompleted(HttpContext context)
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
            var match = await context.Request.JsonBodyAsync<NGameMatchCompletedRequest>();
            if(match == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            int tid = 0;
            string name = "";
            string id = "";

            GameMatchQueueItem? item;
            // 100 是等待，默认返回100
            int result_code = 100;
            //
            result_code = this.DBMatchCompleted(auth_data, match.ID, out item);
            if(result_code == 0)
            {
                this.TryQueuesWorking();
                result_code = 100;
            }
            else if(result_code < 0)
            {

            }
            else if(item != null)
            {
                id = item.server_id;
                tid = item.tid;
                name = item.name;

                _logger?.Log($"{TAGName} Match Completed : {item.tid} {item.name}");
            }
                        
            //
            var result = new NGameMatchCompletedResponse {
                Code = result_code,
                Index = match.Index,
                ID = match.ID,
                PlayerID = id,
                PlayerName = name,
                PlayerTID = tid,
                PlayerType = GameMatchPlayerType.AI
            };

            //
            await context.ResponseResult(result);
        }
    }
}