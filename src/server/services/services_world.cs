using WebSocketSharp;
using WebSocketSharp.Server;
using Protocols.World.Chat;
using Google.Protobuf;
using Logger;


namespace Server.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldService : Services.Base
    {
        protected UserSession? _session = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        protected int AuthenticationUserSession(out UserSession? session)
        {
            string token = this.QueryString["token"] ?? "";
            string text = token.ToString().Trim();
            if (text.Length == 0)
            {
                text = this.Headers["X-Authorization"] ?? "";
                text = text.Trim();
            }

            string key = "";
            string hash = "";

            string[] values = text.Split(":");
            if (values.Length > 0)
            {
                key = values[0].Trim();
            }
            if (values.Length > 1)
            {
                hash = values[1].Trim();
            }

            session = UserManager.Instance.GetAuthenticationSession(key, hash);
            if (session != null)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// 检测纯数字用户ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public bool CheckUserIDN(string ID)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ID, @"^[1-9]\d{5,}$");
        }

        protected override void OnOpen()
        {
            UserSession? session = null;
            if (AuthenticationUserSession(out session) <= 0 || session == null)
            {
                this.Close(CloseStatusCode.PolicyViolation, "Access Denied");
                return;
            }

            //
            session.BindService(this);
            _session = session;

            base.OnOpen();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            //
            if (_session != null)
            {
                _session.FreeService();
                _session = null;
            }
        }

        protected override void OnMessage(MessageEventArgs msg)
        {
            base.OnMessage(msg);

            if (!this.HasPacket || this.PacketData == null)
            {
                return;
            }

            PacketHandleIndex index = (PacketHandleIndex)this._packet_index;

            switch (index)
            {
                case PacketHandleIndex.ChatMessage:
                    this.OnChatMessageResponse(this.GetPacketT<Protocols.World.Chat.ChatMessage>());
                    break;
                case PacketHandleIndex.RoomEnter:
                    this.OnRoomEnterResponse(this.GetPacketT<Protocols.World.Room.RoomEnter>());
                    break;
                default:
                    Logger.LoggerFactory.Instance?.LogError($"[Service] (WorldService) Packet : Unknow Header ({index})");
                    break;
            }


        }

        /// <summary>
        /// 世界聊天
        /// </summary>
        /// <param name="packet"></param>
        protected void OnChatMessageResponse(Protocols.World.Chat.ChatMessage? packet)
        {
            if (packet == null || packet.MessageType > MessageType.System)
            {
                // 来自用户的聊天不能包含系统，通知等
                return;
            }

            // 校验用户ID
            if (packet.UserId != _session?.ID)
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
            response.Timestamp = AMToolkits.Utility.Utils.GetLongTimestamp();

            // 
            response.UserId = _session?.ID;
            response.UserName = packet.UserName;

            //this.Send(response.ToByteArray());
            // 广播给所有用户
            UserManager.Instance.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.ChatMessageResponse);
        }
        
        /// <summary>
        /// 进入房间
        /// </summary>
        /// <param name="packet"></param>
        protected void OnRoomEnterResponse(Protocols.World.Room.RoomEnter? packet)
        {
            if (packet == null)
            {
                // 来自用户的聊天不能包含系统，通知等
                return;
            }

            // 校验用户ID
            if (packet.UserId != _session?.ID)
            {
                return;
            }

            bool is_attached_id = false;
            // 漏洞，有可能会有封包欺骗
            string user_id = packet.UserId;
            if (packet.AttachedId.Length > 0)
            {
                if (!CheckUserIDN(packet.AttachedId))
                {
                    return;
                }

                is_attached_id = true;
                user_id = packet.AttachedId.Trim();
            }
            
            // 构建消息
            var response = new Protocols.World.Room.RoomEnterResponse();
            response.ResultCode = 0;

            string secret_key = packet.AccessToken.Trim();
            string[] values = packet.AccessToken.Trim().Split(":");
            if (values.Length == 1) // key
            {
                secret_key = values[0];
            }
            else if (values.Length == 2) // rid:key
            {
                int rid = 0;
                int.TryParse(values[0], out rid);

                if (packet.RoomId != rid)
                {
                    response.ResultCode = -1;
                }
                secret_key = values[1];
            }
            else if (values.Length == 3) // x:rid:key
            {
                int rid = 0;
                int.TryParse(values[1], out rid);

                if (packet.RoomId != rid)
                {
                    response.ResultCode = -1;
                }
                secret_key = values[2];
            }

            if (response.ResultCode == 0)
            {
                response.ResultCode = RoomManager.Instance.SetPlayerEnterRoom(packet.RoomId, secret_key, user_id);
                if (response.ResultCode == 0)
                {
                    Logger.LoggerFactory.Instance?.LogWarning($"[Service] (WorldService) Room : ({packet.RoomId}) (ID:{user_id}) Enter Not Allow");
                }
            }

            //
            response.RoomId = packet.RoomId;

            //
            response.Timestamp = AMToolkits.Utility.Utils.GetLongTimestamp();

            // 
            response.UserId = user_id;
            //response.ResultCode = 1;
            this.SendData(response.ToByteArray(), (int)PacketHandleIndex.RoomEnterResponse);
        }
    }
}