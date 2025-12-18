using WebSocketSharp;
using Protocols.World.Chat;
using Google.Protobuf;
using Logger;


namespace Server.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldService : Services.Base, Services.ISession
    {
        private string _session_id = "";
        private string _user_id = "";
        public string SessionID => _session_id;
        public string UserID => _user_id;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void InitSession(string id, string user_id)
        {
            _session_id = id;
            _user_id = user_id;
        }

        public void FreeSession()
        {
            _session_id = "";
            _user_id = "";
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
            base.OnOpen();

            //
            if(World.WorldServer.Instance.DoAccept(this) <= 0)
            {
                return;
            }

        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            //
            World.WorldServer.Instance.DoClose(this);
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
                    World.WorldServer.Instance.HandleChatMessage(this, this.GetPacketT<Protocols.World.Chat.ChatMessage>());
                    break;

                ////
                case PacketHandleIndex.GMNotice:
                    World.WorldServer.Instance.HandleGMNotice(this, this.GetPacketT<Protocols.World.Admin.GMNoticeRequest>());
                    break;
                default:
                    Logger.LoggerFactory.Instance?.LogError($"[Service] (WorldService) Packet : Unknow Header (0x{index:X})");
                    break;
            }


        }

        /// <summary>
        /// 进入房间
        /// </summary>
        /// <param name="packet"></param>
        // protected void OnRoomEnterResponse(Protocols.World.Room.RoomEnter? packet)
        // {
        //     if (packet == null)
        //     {
        //         // 来自用户的聊天不能包含系统，通知等
        //         return;
        //     }

        //     // 校验用户ID
        //     if (packet.UserId != _user_id)
        //     {
        //         return;
        //     }

        //     bool is_attached_id = false;
        //     // 漏洞，有可能会有封包欺骗
        //     string user_id = packet.UserId;
        //     if (packet.AttachedId.Length > 0)
        //     {
        //         if (!CheckUserIDN(packet.AttachedId))
        //         {
        //             return;
        //         }

        //         is_attached_id = true;
        //         user_id = packet.AttachedId.Trim();
        //     }

        //     // 构建消息
        //     var response = new Protocols.World.Room.RoomEnterResponse();
        //     response.ResultCode = 0;

        //     string secret_key = packet.AccessToken.Trim();
        //     string[] values = packet.AccessToken.Trim().Split(":");
        //     if (values.Length == 1) // key
        //     {
        //         secret_key = values[0];
        //     }
        //     else if (values.Length == 2) // rid:key
        //     {
        //         int rid = 0;
        //         int.TryParse(values[0], out rid);

        //         if (packet.RoomId != rid)
        //         {
        //             response.ResultCode = -1;
        //         }
        //         secret_key = values[1];
        //     }
        //     else if (values.Length == 3) // x:rid:key
        //     {
        //         int rid = 0;
        //         int.TryParse(values[1], out rid);

        //         if (packet.RoomId != rid)
        //         {
        //             response.ResultCode = -1;
        //         }
        //         secret_key = values[2];
        //     }

        //     if (response.ResultCode == 0)
        //     {
        //         response.ResultCode = RoomManager.Instance.SetPlayerEnterRoom(packet.RoomId, secret_key, user_id);
        //         if (response.ResultCode == 0)
        //         {
        //             Logger.LoggerFactory.Instance?.LogWarning($"[Service] (WorldService) Room : ({packet.RoomId}) (ID:{user_id}) Enter Not Allow");
        //         }

        //         _room_id = packet.RoomId;
        //         _room_player_ids.Add(user_id);
        //     }

        //     //
        //     response.RoomId = packet.RoomId;

        //     //
        //     response.Timestamp = AMToolkits.Utils.GetLongTimestamp();

        //     // 
        //     response.UserId = user_id;
        //     //response.ResultCode = 1;
        //     this.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.RoomEnterResponse);
        // }

        /// <summary>
        /// 离开房间
        /// </summary>
        /// <param name="packet"></param>
        // protected void OnRoomLeaveResponse(Protocols.World.Room.RoomLeave? packet)
        // {
        //     if (packet == null)
        //     {
        //         // 来自用户的聊天不能包含系统，通知等
        //         return;
        //     }

        //     // 校验用户ID
        //     if (packet.UserId != _user_id)
        //     {
        //         return;
        //     }

        //     bool is_attached_id = false;
        //     // 漏洞，有可能会有封包欺骗
        //     string user_id = packet.UserId.Trim();
        //     if (packet.AttachedId.Length > 0)
        //     {
        //         if (!CheckUserIDN(packet.AttachedId))
        //         {
        //             return;
        //         }

        //         is_attached_id = true;
        //         user_id = packet.AttachedId.Trim();
        //     }

        //     // 构建消息
        //     var response = new Protocols.World.Room.RoomLeaveResponse();
        //     response.ResultCode = 0;

        //     response.ResultCode = this.PlayerLeaveRoom(packet.RoomId, user_id);

        //     _room_player_ids.Remove(user_id);

        //     //
        //     response.RoomId = packet.RoomId;

        //     //
        //     response.Timestamp = AMToolkits.Utils.GetLongTimestamp();

        //     // 
        //     response.UserId = user_id;
        //     //response.ResultCode = 1;
        //     this.BroadcastAsync(response.ToByteArray(), (int)PacketHandleIndex.RoomLeaveResponse);
        // }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="user_id"></param>
        /// <param name="force">强制离开房间</param>
        /// <returns></returns>
        // private int PlayerLeaveRoom(int rid, string user_id, bool force = false)
        // {
        //     int result_code = RoomManager.Instance.SetPlayerLeaveRoom(rid, user_id);
        //     if (result_code == 0)
        //     {
        //         Logger.LoggerFactory.Instance?.LogWarning($"[Service] (WorldService) Room : ({rid}) (ID:{user_id}) Leave is NULL");
        //     }
        //     if (force)
        //     {
        //         Logger.LoggerFactory.Instance?.LogWarning($"[Service] (WorldService) Room : ({rid}) (ID:{user_id}) Leave (Force)");
        //     }
        //     return result_code;
        // }
    }
}