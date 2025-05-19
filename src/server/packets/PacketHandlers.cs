

namespace Server.Services
{
    public enum PacketHandleIndex
    {
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
    }
}
