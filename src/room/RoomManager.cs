using System.Text.Json.Serialization;
using AMToolkits.Utility;
using Logger;

namespace Server
{
    [System.Serializable]
    public class RoomData
    {
        [JsonPropertyName("rid")]
        public int RID = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class RoomManager : SingletonT<RoomManager>, ISingleton
    {
        [AutoInitInstance]
        protected static RoomManager? _instance;
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;


        protected int _room_count = 0;
        // 将 Random 实例提升为类成员变量，避免重复创建
        private readonly System.Random _rand = new System.Random();

        public RoomManager()
        {

        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[RoomManager] Config is NULL.");
                return ;
            }
            _config = config;
            

            this.InitLogger();

        }


        private void InitLogger()
        {
            //
            _logger = null;
            var cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "room");
            if(cfg != null) {
                if(!cfg.Enabled) {
                    _logger = null;
                }
                else
                {
                    _logger = Logger.LoggerFactory.CreateLogger(cfg.Name, cfg.IsConsole, cfg.IsFile);
                    _logger.SetOutputFileName(cfg.File);
                }
            }
        }

        public int StartWorking()
        {
            // 初始化房间
            // 获取当前数据库中的房间数量
            _room_count = this.DBRoomCount();
            if(_room_count < _config?.Room.RoomMaxNum)
            {
                if(!this.CreateRooms(_config.Room.RoomMaxNum - _room_count))
                {
                    _logger?.LogError("[Room] Init Rooms Failed");
                    return -1;
                }
            }

            // 统一初始化
            this.InitRooms();

            //
            _room_count = this.DBRoomCount();
            _logger?.Log($"{TAGName} (Room) MaxCount:{_room_count}");

            //
            this.ProcessWorking();
            return 0;
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

        private bool CreateRooms(int count = 0)
        {
            int num = _config?.Room.PlayersMaxNum ?? 2;
            for(int i = 0; i < count; i ++)
            {
                int rid = GeneratorID6XN();
                if(this.DBRoomCreateOne(rid, num) <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool InitRooms()
        {
            if(this.DBRoomsInit() <= 0)
            {
                return false;
            }
            return true;
        }

        public RoomData? GetIdleRoom()
        {
            RoomData room = new RoomData() {
                RID = -1,
            };
            if(this.DBRoomIdle(room) <= 0 || room.RID <= 0)
            {
                return null;
            }
            return room;
        }

        /// <summary>
        /// 内部设置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="player_0"></param>
        /// <param name="player_1"></param>
        /// <returns></returns>
        public int SetRoomWithMatch(RoomData room, GameMatchQueueItem player_0, GameMatchQueueItem player_1)
        {
            if(this.DBRoomSet(room, player_0.server_id, player_1.server_id) <= 0)
            {
                return -1;
            }
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<int> ProcessWorking()
        {
            if(_config?.Room.Enabled == false)
            {
                _logger?.Log("[RoomManager] Not Enabled");
                return 0;
            }
            
            _logger?.Log("[RoomManager] Start Working");
            
            //
            return 0;
        }
    }
}