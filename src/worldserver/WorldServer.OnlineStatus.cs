using System.Collections.Concurrent;
using AMToolkits.Extensions;
using Server.Services;
using Google.Protobuf;
using Logger;

namespace Server.World
{
    /// <summary>
    /// 玩家在线状态数据
    /// </summary>
    public class PlayerOnlineData
    {
        public string UserID { get; set; } = "";
        public string SessionID { get; set; } = "";
        public string UserName { get; set; } = ""; // 显示名称
        public int Level { get; set; } = 0;        // 玩家等级
        public int Status { get; set; } = 0;       // 0:离线,1:在线,2:忙碌,3:离开
        public long LastHeartbeat { get; set; } = 0; // 最后心跳时间戳
        public long LoginTime { get; set; } = 0;     // 登录时间
        public string CurrentRoomID { get; set; } = ""; // 当前房间ID
    }

    /// <summary>
    /// WorldServer 在线状态管理扩展
    /// </summary>
    public partial class WorldServer
    {
        // 在线玩家数据字典
        protected ConcurrentDictionary<string, PlayerOnlineData> _online_players = 
            new ConcurrentDictionary<string, PlayerOnlineData>();

        // 心跳超时阈值（毫秒）
        private const long HEARTBEAT_TIMEOUT_MS = 60 * 1000; // 60秒

        /// <summary>
        /// 检查玩家是否在线
        /// </summary>
        public bool IsPlayerOnline(string userId)
        {
            return _online_players.ContainsKey(userId);
        }

        /// <summary>
        /// 获取玩家在线状态数据
        /// </summary>
        public PlayerOnlineData? GetPlayerOnlineData(string userId)
        {
            _online_players.TryGetValue(userId, out var data);
            return data;
        }

        /// <summary>
        /// 获取玩家的WebSocket服务
        /// </summary>
        public Services.WorldService? GetPlayerService(string userId)
        {
            if (_online_players.TryGetValue(userId, out var playerData))
            {
                if (_list.TryGetValue(playerData.SessionID, out var service))
                {
                    return service;
                }
            }
            return null;
        }

        /// <summary>
        /// 添加在线玩家
        /// </summary>
        public bool AddOnlinePlayer(string sessionID, string userID, string userName = "", int level = 0)
        {
            var playerData = new PlayerOnlineData
            {
                UserID = userID,
                SessionID = sessionID,
                UserName = userName,
                Level = level,
                Status = 1, // 在线
                LastHeartbeat = AMToolkits.Utils.GetLongTimestamp(),
                LoginTime = AMToolkits.Utils.GetLongTimestamp()
            };

            _online_players.AddOrUpdate(userID, playerData, (k, v) => playerData);
            return true;
        }

        /// <summary>
        /// 移除在线玩家
        /// </summary>
        public bool RemoveOnlinePlayer(string userID)
        {
            return _online_players.TryRemove(userID, out _);
        }

        /// <summary>
        /// 更新玩家心跳时间
        /// </summary>
        public void UpdatePlayerHeartbeat(string userID, int status = -1)
        {
            if (_online_players.TryGetValue(userID, out var playerData))
            {
                playerData.LastHeartbeat = AMToolkits.Utils.GetLongTimestamp();
                if (status >= 0)
                {
                    playerData.Status = status;
                }
            }
        }

        /// <summary>
        /// 更新玩家状态
        /// </summary>
        public void UpdatePlayerStatus(string userID, int newStatus)
        {
            if (_online_players.TryGetValue(userID, out var playerData))
            {
                int oldStatus = playerData.Status;
                playerData.Status = newStatus;
                
                // 广播状态变化通知
                if (oldStatus != newStatus)
                {
                    _ = BroadcastPlayerStatusChange(playerData, oldStatus, newStatus);
                }
            }
        }

        /// <summary>
        /// 获取所有在线玩家
        /// </summary>
        public List<PlayerOnlineData> GetAllOnlinePlayers()
        {
            return _online_players.Values.ToList();
        }

        /// <summary>
        /// 获取在线玩家数量
        /// </summary>
        public int GetOnlinePlayerCount()
        {
            return _online_players.Count;
        }

        /// <summary>
        /// 检查并清理超时的心跳
        /// </summary>
        public void CheckHeartbeatTimeouts()
        {
            var currentTime = AMToolkits.Utils.GetLongTimestamp();
            var timeoutPlayers = new List<string>();

            foreach (var pair in _online_players)
            {
                if (currentTime - pair.Value.LastHeartbeat > HEARTBEAT_TIMEOUT_MS)
                {
                    timeoutPlayers.Add(pair.Key);
                }
            }

            foreach (var userID in timeoutPlayers)
            {
                var service = GetPlayerService(userID);
                if (service != null)
                {
                    _logger?.LogWarning($"[WorldServer] Player {userID} heartbeat timeout, disconnecting");
                    service.DoClose(WebSocketSharp.CloseStatusCode.Abnormal, "heartbeat_timeout");
                }
            }
        }

        /// <summary>
        /// 广播玩家上线通知
        /// </summary>
        private async Task BroadcastPlayerOnlineNotify(PlayerOnlineData playerData)
        {
            try
            {
                var notify = new Protocols.World.Presence.PlayerOnlineNotify();
                notify.UserId = playerData.UserID;
                notify.UserName = playerData.UserName;
                notify.Level = playerData.Level;
                notify.Timestamp = AMToolkits.Utils.GetLongTimestamp();

                await BroadcastAsync(notify.ToByteArray(), (int)PacketHandleIndex.PlayerOnlineNotify);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[WorldServer] Broadcast online notify error: {ex.Message}");
            }
        }

        /// <summary>
        /// 广播玩家下线通知
        /// </summary>
        private async Task BroadcastPlayerOfflineNotify(PlayerOnlineData playerData)
        {
            try
            {
                var notify = new Protocols.World.Presence.PlayerOfflineNotify();
                notify.UserId = playerData.UserID;
                notify.UserName = playerData.UserName;
                notify.Timestamp = AMToolkits.Utils.GetLongTimestamp();

                await BroadcastAsync(notify.ToByteArray(), (int)PacketHandleIndex.PlayerOfflineNotify);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[WorldServer] Broadcast offline notify error: {ex.Message}");
            }
        }

        /// <summary>
        /// 广播玩家状态变化通知
        /// </summary>
        private async Task BroadcastPlayerStatusChange(PlayerOnlineData playerData, int oldStatus, int newStatus)
        {
            try
            {
                var notify = new Protocols.World.Presence.PlayerStatusChangeNotify();
                notify.UserId = playerData.UserID;
                notify.UserName = playerData.UserName;
                notify.OldStatus = oldStatus;
                notify.NewStatus = newStatus;
                notify.Timestamp = AMToolkits.Utils.GetLongTimestamp();

                await BroadcastAsync(notify.ToByteArray(), (int)PacketHandleIndex.PlayerStatusChangeNotify);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[WorldServer] Broadcast status change error: {ex.Message}");
            }
        }
    }
}