
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
    }
}