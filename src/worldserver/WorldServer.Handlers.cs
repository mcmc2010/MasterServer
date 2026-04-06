
using System.Threading.Tasks;
using Protocols.World.Chat;
using Google.Protobuf;
using Server.Services;

using Logger;



namespace Server.World
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WorldServer
    {
        #region Handlers - GM
        public async Task HandleGMNotice(Services.WorldService service, Protocols.World.Admin.GMNoticeRequest? packet)
        {
            if (packet == null)
            {
                // 来自用户的聊天不能包含系统，通知等
                return;
            }

            // 校验用户ID
            if (packet.UserId != service.UserID)
            {
                return;
            }


            if (!UserManager.Instance._CheckUserPrivilegeLevel(service.UserID, PrivilegeLevel.Master))
            {
                Logger.LoggerFactory.Instance?.LogWarning($"[Service] (WorldService) Admin : (ID:{service.UserID}) Not Allow, No Permission");
                return;
            }

            // 构建消息
            var response = new Protocols.World.Admin.GMNoticeResponse();

            //
            response.NoticeId = AMToolkits.Utility.Guid.GeneratorID12();
            response.Level = packet.Level;

            //
            response.Content = packet.Content;
            response.Timestamp = AMToolkits.Utils.GetLongTimestamp();

            // 
            response.UserId = service.UserID;
            //response.Name = packet.Name;

            // 广播给所有用户
            await World.WorldServer.Instance.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.GMNoticeResponse);
        }
        #endregion

        #region Handlers - World
        /// <summary>
        /// 世界聊天
        /// </summary>
        /// <param name="service"></param>
        /// <param name="packet"></param>
        public async Task HandleChatMessage(Services.WorldService service, Protocols.World.Chat.ChatMessage? packet)
        {

            if (packet == null || packet.MessageType > MessageType.System)
            {
                // 来自用户的聊天不能包含系统，通知等
                return;
            }

            // 校验用户ID
            if (packet.UserId != service.UserID)
            {
                return;
            }

            // 构建消息
            var response = new Protocols.World.Chat.ChatMessageResponse();

            //
            response.MessageId = AMToolkits.Utility.Guid.GeneratorID12();
            response.MessageType = packet.MessageType;

            //
            response.Content = packet.Content;
            response.Timestamp = AMToolkits.Utils.GetLongTimestamp();

            // 
            response.UserId = service.UserID;
            response.UserName = packet.UserName;

            // 广播给所有用户
            await this.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.ChatMessageResponse);
        }

        #endregion

        #region Handlers - Heartbeat & Online Status
        /// <summary>
        /// 处理心跳请求
        /// </summary>
        public async Task HandleHeartbeat(Services.WorldService service, Protocols.World.Heartbeat.HeartbeatRequest? packet)
        {
            if (packet == null) return;

            // 更新玩家心跳时间
            UpdatePlayerHeartbeat(service.UserID, packet.Status);

            // 发送心跳响应
            var response = new Protocols.World.Heartbeat.HeartbeatResponse();
            response.Timestamp = AMToolkits.Utils.GetLongTimestamp();
            response.ClientTimestamp = packet.Timestamp;
            response.ResultCode = 0;

            await service.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.HeartbeatResponse);
        }

        /// <summary>
        /// 处理玩家在线状态查询
        /// </summary>
        public async Task HandlePlayerOnlineStatusRequest(Services.WorldService service, Protocols.World.Heartbeat.PlayerOnlineStatusRequest? packet)
        {
            if (packet == null) return;

            var response = new Protocols.World.Heartbeat.PlayerOnlineStatusResponse();
            response.UserId = packet.UserId;
            response.ResultCode = 0;

            var playerData = GetPlayerOnlineData(packet.UserId);
            if (playerData != null)
            {
                response.IsOnline = true;
                response.Status = playerData.Status;
                response.LastSeen = playerData.LastHeartbeat;
            }
            else
            {
                response.IsOnline = false;
                response.Status = 0;
                response.LastSeen = 0;
            }

            await service.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.PlayerOnlineStatusResponse);
        }

        /// <summary>
        /// 处理批量在线状态查询
        /// </summary>
        public async Task HandleBatchOnlineStatusRequest(Services.WorldService service, Protocols.World.Heartbeat.BatchOnlineStatusRequest? packet)
        {
            if (packet == null) return;

            var response = new Protocols.World.Heartbeat.BatchOnlineStatusResponse();
            response.ResultCode = 0;

            foreach (var userId in packet.UserIds)
            {
                var playerStatus = new Protocols.World.Heartbeat.PlayerOnlineStatusResponse();
                playerStatus.UserId = userId;
                playerStatus.ResultCode = 0;

                var playerData = GetPlayerOnlineData(userId);
                if (playerData != null)
                {
                    playerStatus.IsOnline = true;
                    playerStatus.Status = playerData.Status;
                    playerStatus.LastSeen = playerData.LastHeartbeat;
                }
                else
                {
                    playerStatus.IsOnline = false;
                    playerStatus.Status = 0;
                    playerStatus.LastSeen = 0;
                }

                response.Players.Add(playerStatus);
            }

            await service.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.BatchOnlineStatusResponse);
        }
        #endregion
    }
}