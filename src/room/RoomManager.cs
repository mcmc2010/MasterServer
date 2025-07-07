using System.Text.Json.Serialization;
using Logger;

namespace Server
{
    [System.Serializable]
    public class RoomData
    {
        [JsonPropertyName("rid")]
        public int RID = 0;
        [JsonPropertyName("rcode")]
        public int RCODE = 0;
        [JsonPropertyName("cur_num")]
        public int CurNum = 0;
        [JsonPropertyName("max_num")]
        public int MaxNum = 0;
        [JsonPropertyName("service_id")]
        public int ServiceID = 0;
        [JsonPropertyName("secret_key")]
        public string SecretKey = "";
    }

    [System.Serializable]
    public class RoomPlayerData
    {
        [JsonPropertyName("id")]
        public string ID = "";
        [JsonPropertyName("rid")]
        public int RID = 0;
        [JsonPropertyName("service_id")]
        public int ServiceID = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class RoomManager : AMToolkits.SingletonT<RoomManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
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
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

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
            int service_id = 0;

            int num = _config?.Room.PlayersMaxNum ?? 2;
            for(int i = 0; i < count; i ++)
            {
                int rid = GeneratorID6XN();
                if(this.DBRoomCreate(rid, num, service_id) <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 初始化关联服务的房间数据
        /// </summary>
        /// <returns></returns>
        public bool InitRooms()
        {
            int service_id = 0;
            int num = _config?.Room.PlayersMaxNum ?? 2;

            if(this.DBRoomsInit(num, service_id) <= 0)
            {
                return false;
            }
            return true;
        }

        public RoomData? GetRoomData(int rid = -1)
        {
            //
            RoomData room = new RoomData()
            {
                RID = rid,
                ServiceID = 0
            };

            //
            if (this.DBGetRoomData(room) <= 0 || room.RID <= 0)
            {
                return null;
            }
            return room;
        }

        public RoomData? GetIdleRoom()
        {
            RoomData room = new RoomData()
            {
                RID = -1,
                ServiceID = 0
            };
            if (this.DBGetIdleRoom(room) <= 0 || room.RID <= 0)
            {
                return null;
            }
            return room;
        }

        public RoomData? SetIdleRoom(int rid)
        {
            RoomData room = new RoomData() {
                RID = rid,
                ServiceID = 0
            };
            if(this.DBSetIdleRoom(room) <= 0)
            {
                return null;
            }
            return room;
        }

        public int SetPlayerEnterRoom(int rid, string secret_key, string user_id)
        {
            RoomData room = new RoomData()
            {
                RID = rid,
                SecretKey = secret_key.Trim(),
                ServiceID = 0
            };

            int result_code = this.DBSetPlayerEnterRoom(room, user_id);
            if(result_code < 0)
            {
                return -1;
            }
            return result_code;
        }


        public int SetPlayerLeaveRoom(int rid, string user_id)
        {
            RoomData room = new RoomData()
            {
                RID = rid,
                ServiceID = 0
            };

            int result_code = this.DBSetPlayerLeaveRoom(room, user_id);
            if(result_code < 0)
            {
                return -1;
            }
            return result_code;
        }

        /// <summary>
        /// 内部设置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="player_0"></param>
        /// <param name="player_1"></param>
        /// <returns></returns>
        public int SetPlayersInRoomWithMatch(RoomData room, params GameMatchQueueItem[] player_list)
        {
            int count = 0;
            //
            foreach (var player in player_list)
            {
                var player_data = new RoomPlayerData()
                {
                    RID = room.RID,
                    ID = player.server_id,
                    ServiceID = room.ServiceID
                };
                if (player.role == GameMatchRoomRole.Master)
                {
                    // 设置房主
                    if (this.DBSetMasterPlayerInRoom(room, player_data) <= 0)
                    {
                        count = -1; break;
                    }
                }
                else
                {
                    // 设置玩家
                    if (this.DBSetPlayerInRoom(room, player_data) <= 0)
                    {
                        count = -1; break;
                    }
                }

                count++;
            }
            return count;
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

            this.GetIdleRoom();

            //
            return 0;
        }
    }
}