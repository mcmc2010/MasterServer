using System.Collections.Concurrent;
using AMToolkits.Extensions;


namespace Server.Field
{
    /// <summary>
    /// 房间管理器
    /// </summary>
    public class RoomManager : IDisposable
    {
        /// <summary>
        /// 帧率为30帧
        /// </summary>
        public static int FrameRate { get; set; } = 30;
        public static int PlayersMaxNum { get; set; } = 20;

        private bool _is_disposed = false;
               
        //
        protected int _room_count = 0;
        // 将 Random 实例提升为类成员变量，避免重复创建
        private readonly System.Random _rand = new System.Random();
        private readonly ConcurrentDictionary<int, Field.Room> _rooms;

        public RoomManager()
        {
            _rooms = new ConcurrentDictionary<int, Field.Room>();
        }

        public void Dispose()
        {
            if (_is_disposed)
            {
                return;
            }

            _is_disposed = true;

            //
            foreach (var room in _rooms.Values)
            {
                room.Dispose();
            }
            _rooms.Clear();
        }

        /// <summary>
        /// ID8N
        /// </summary>
        /// <returns></returns>
        public int GeneratorID6XN()
        {
            var now = DateTime.UtcNow;
            int NR = _rand.Next(10000, 9999999);
            string rid = $"1{NR}";
            return int.Parse(rid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="maxPlayers"></param>
        /// <param name="hostConnectionId"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public int CreateRoom(string name, int players_maxnum = 20)
        {
            var rid = GeneratorID6XN();

            if (name.IsNullOrWhiteSpace())
            {
                name = $"Room_{rid}";
            }
            name = name.Trim();

            var room = new Field.Room(rid, $"Room_{rid}", players_maxnum);
            _rooms[rid] = room;

            //
            return rid;
        }

        public bool CloseRoom(int rid)
        {
            if (!_rooms.TryGetValue(rid, out var room) || room == null)
            {
                return false;
            }

            room.RemoveAllPlayer().Wait(1000);


            _rooms.TryRemove(rid, out _);
            room.Dispose();           
            return true;
        }

        internal void UpdateHeartbeat()
        {
            foreach (var room in _rooms.Values)
            {
                room.CheckHeartbeat();
            }
        }

    }
}