using System.Collections.Concurrent;



namespace Server.Field
{
    /// <summary>
    /// 房间状态
    /// </summary>
    public enum RoomStatus
    {
        None,
        Waiting,    // 等待玩家
        Ready,      // 准备开始
        Running,    // 运行中
        Paused,     // 暂停
        Ended       // 结束
    }
    

    /// <summary>
    /// 房间
    /// </summary>
    public class Room : IDisposable
    {
        private bool _is_disposed = false;

        private long _frame_interval = 30;

        private Field.RoomStatus _status = Field.RoomStatus.None;

        private readonly ConcurrentDictionary<string, Field.PlayerData> _players;

        //
        private readonly int _room_id;
        private readonly string _room_name;

        private readonly int _players_maxnum;

        private int _current_players = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="max_players"></param>
        public Room(int id, string name, int players_maxnum = 20)
        {
            _frame_interval = (long)(1000.0f / RoomManager.FrameRate);

            //
            _room_id = id;
            _room_name = name;
            _players_maxnum = players_maxnum;

            _players = new ConcurrentDictionary<string, PlayerData>();
        }

        public void Dispose()
        {
            if (_is_disposed)
            {
                return;
            }

            _is_disposed = true;
        }

        /// <summary>
        /// 逐个断开玩家
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task RemoveAllPlayer()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void CheckHeartbeat()
        {
            if (_is_disposed || _players.Count == 0)
            {
                return;
            }


        }
    }
}