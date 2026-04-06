using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Logger;

namespace Server
{
    #region Online Status Request/Response
    [System.Serializable]
    public class NUserOnlineStatusRequest
    {
        [JsonPropertyName("user_id")]
        public string UserID = "";
    }

    [System.Serializable]
    public class NUserOnlineStatusResponse
    {
        [JsonPropertyName("code")]
        public int Code = 0;
        
        [JsonPropertyName("user_id")]
        public string UserID = "";
        
        [JsonPropertyName("is_online")]
        public bool IsOnline = false;
        
        [JsonPropertyName("status")]
        public int Status = 0; // 0:离线,1:在线,2:忙碌,3:离开
        
        [JsonPropertyName("last_seen")]
        public long LastSeen = 0;
    }

    [System.Serializable]
    public class NBatchOnlineStatusRequest
    {
        [JsonPropertyName("user_ids")]
        public List<string> UserIDs = new List<string>();
    }

    [System.Serializable]
    public class NBatchOnlineStatusResponse
    {
        [JsonPropertyName("code")]
        public int Code = 0;
        
        [JsonPropertyName("players")]
        public List<NUserOnlineStatusResponse> Players = new List<NUserOnlineStatusResponse>();
    }

    [System.Serializable]
    public class NOnlineCountResponse
    {
        [JsonPropertyName("code")]
        public int Code = 0;
        
        [JsonPropertyName("count")]
        public int Count = 0;
    }
    #endregion

    /// <summary>
    /// UserManager 在线状态查询扩展
    /// </summary>
    public partial class UserManager
    {
        /// <summary>
        /// 注册在线状态相关的HTTP API
        /// </summary>
        public void RegisterOnlineStatusHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[UserManager] Register Online Status Handlers");

            args.app?.MapPost("api/user/online/status", HandleGetUserOnlineStatus);
            args.app?.MapPost("api/user/online/batch", HandleGetBatchOnlineStatus);
            args.app?.MapGet("api/user/online/count", HandleGetOnlineCount);
        }

        /// <summary>
        /// 处理单个玩家在线状态查询
        /// </summary>
        private async Task HandleGetUserOnlineStatus(HttpContext context)
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<NUserOnlineStatusRequest>();
                if (request == null || string.IsNullOrEmpty(request.UserID))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                // 检查玩家是否在线
                var isOnline = World.WorldServer.Instance.IsPlayerOnline(request.UserID);
                var playerData = World.WorldServer.Instance.GetPlayerOnlineData(request.UserID);

                var response = new NUserOnlineStatusResponse
                {
                    Code = 0,
                    UserID = request.UserID,
                    IsOnline = isOnline,
                    Status = playerData?.Status ?? 0,
                    LastSeen = playerData?.LastHeartbeat ?? 0
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                Logger.LoggerFactory.Instance?.LogError($"[UserManager] Online Status Error: {ex.Message}");
                context.Response.StatusCode = 500;
            }
        }

        /// <summary>
        /// 处理批量玩家在线状态查询
        /// </summary>
        private async Task HandleGetBatchOnlineStatus(HttpContext context)
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<NBatchOnlineStatusRequest>();
                if (request == null || request.UserIDs == null || request.UserIDs.Count == 0)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                var response = new NBatchOnlineStatusResponse
                {
                    Code = 0,
                    Players = new List<NUserOnlineStatusResponse>()
                };

                foreach (var userId in request.UserIDs)
                {
                    var isOnline = World.WorldServer.Instance.IsPlayerOnline(userId);
                    var playerData = World.WorldServer.Instance.GetPlayerOnlineData(userId);

                    var playerStatus = new NUserOnlineStatusResponse
                    {
                        Code = 0,
                        UserID = userId,
                        IsOnline = isOnline,
                        Status = playerData?.Status ?? 0,
                        LastSeen = playerData?.LastHeartbeat ?? 0
                    };

                    response.Players.Add(playerStatus);
                }

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                Logger.LoggerFactory.Instance?.LogError($"[UserManager] Batch Online Status Error: {ex.Message}");
                context.Response.StatusCode = 500;
            }
        }

        /// <summary>
        /// 获取在线玩家数量
        /// </summary>
        private async Task HandleGetOnlineCount(HttpContext context)
        {
            try
            {
                var count = World.WorldServer.Instance.GetOnlinePlayerCount();

                var response = new NOnlineCountResponse
                {
                    Code = 0,
                    Count = count
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                Logger.LoggerFactory.Instance?.LogError($"[UserManager] Online Count Error: {ex.Message}");
                context.Response.StatusCode = 500;
            }
        }
    }
}