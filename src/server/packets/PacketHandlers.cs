

namespace Server.Services
{
    public enum PacketHandleIndex
    {
        // Heartbeat & Online Status
        Heartbeat = 0x0010,
        HeartbeatResponse = 0x0011,
        PlayerOnlineStatusRequest = 0x0012,
        PlayerOnlineStatusResponse = 0x0013,
        BatchOnlineStatusRequest = 0x0014,
        BatchOnlineStatusResponse = 0x0015,

        // Presence Notifications
        PlayerOnlineNotify = 0x0020,
        PlayerOfflineNotify = 0x0021,
        PlayerStatusChangeNotify = 0x0022,
        FriendsOnlineListRequest = 0x0023,
        FriendsOnlineListResponse = 0x0024,

        // Chat
        ChatMessage = 0x0100,
        ChatMessageResponse = 0x0101,

        // Room
        RoomCreate = 0x2010,
        RoomCreateResponse = 0x2011,
        RoomEnter = 0x2020,
        RoomEnterResponse = 0x2021,
        RoomLeave = 0x2030,
        RoomLeaveResponse = 0x2031,

        // Admin
        GMNotice = 0x7000,
        GMNoticeResponse = 0x7001
    }
}
