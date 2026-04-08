using System.Collections.Concurrent;
using AMToolkits.Extensions;
using Logger;


namespace Server.Field
{
    /// <summary>
    /// 房间管理器 - 基于Nakama帧同步机制
    /// </summary>
    public class RoomManager : IDisposable
    {
        /// <summary>
        /// 帧率为30帧
        /// </summary>
        public static int FrameRate { get; set; } = 30;
        public static int PlayersMaxNum { get; set; } = 20;

        private bool _is_disposed = false;
        private readonly Logger.LoggerEntry? _logger = Logger.LoggerFactory.Instance;
               
        // 房间计数
        protected int _room_count = 0;
        
        // 随机数生成器
        private readonly System.Random _rand = new System.Random();
        
        // 房间字典
        private readonly ConcurrentDictionary<int, Field.Room> _rooms;

        // 玩家房间映射（玩家ID -> 房间ID）
        private readonly ConcurrentDictionary<string, int> _player_room_mapping = new();

        // 空闲房间列表
        private readonly ConcurrentQueue<int> _idle_rooms = new();

        public RoomManager()
        {
            _rooms = new ConcurrentDictionary<int, Field.Room>();
            _logger?.Log("[RoomManager] Initialized");
        }

        public void Dispose()
        {
            if (_is_disposed)
            {
                return;
            }

            _is_disposed = true;

            // 关闭所有房间
            foreach (var room in _rooms.Values)
            {
                room.Dispose();
            }
            _rooms.Clear();
            _player_room_mapping.Clear();
            
            _logger?.Log("[RoomManager] Disposed");
        }

        #region 房间创建和管理

        /// <summary>
        /// 生成房间ID
        /// </summary>
        /// <returns>房间ID</returns>
        public int GeneratorID6XN()
        {
            var now = DateTime.UtcNow;
            int NR = _rand.Next(10000, 9999999);
            string rid = $"1{NR}";
            return int.Parse(rid);
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="name">房间名称</param>
        /// <param name="players_maxnum">最大玩家数</param>
        /// <returns>房间ID</returns>
        public int CreateRoom(string name, int players_maxnum = 20)
        {
            var rid = GeneratorID6XN();

            if (name.IsNullOrWhiteSpace())
            {
                name = $"Room_{rid}";
            }
            name = name.Trim();

            var room = new Field.Room(rid, name, players_maxnum);
            
            // 订阅房间事件
            room.OnPlayerJoin += OnPlayerJoinRoom;
            room.OnPlayerLeave += OnPlayerLeaveRoom;
            room.OnGameStart += () => OnGameStart(rid);
            room.OnGameEnd += () => OnGameEnd(rid);
            
            _rooms[rid] = room;
            _room_count++;

            _logger?.Log($"[RoomManager] Room {rid} created with name '{name}' and max players {players_maxnum}");
            
            return rid;
        }

        /// <summary>
        /// 关闭房间
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <returns>是否成功</returns>
        public bool CloseRoom(int rid)
        {
            if (!_rooms.TryGetValue(rid, out var room) || room == null)
            {
                return false;
            }

            // 停止游戏
            room.StopGame();
            
            // 移除所有玩家
            room.RemoveAllPlayer().Wait(1000);

            // 取消订阅事件
            room.OnPlayerJoin -= OnPlayerJoinRoom;
            room.OnPlayerLeave -= OnPlayerLeaveRoom;

            // 移除房间
            _rooms.TryRemove(rid, out _);
            room.Dispose();
            
            _room_count--;
            
            _logger?.Log($"[RoomManager] Room {rid} closed");
            
            return true;
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <returns>房间对象</returns>
        public Room? GetRoom(int rid)
        {
            _rooms.TryGetValue(rid, out var room);
            return room;
        }

        /// <summary>
        /// 获取所有房间
        /// </summary>
        /// <returns>房间列表</returns>
        public List<Room> GetAllRooms()
        {
            return _rooms.Values.ToList();
        }

        /// <summary>
        /// 获取空闲房间
        /// </summary>
        /// <returns>空闲房间</returns>
        public Room? GetIdleRoom()
        {
            // 尝试从空闲队列获取
            while (_idle_rooms.TryDequeue(out var rid))
            {
                if (_rooms.TryGetValue(rid, out var room) && 
                    room.Status == RoomStatus.Waiting && 
                    room.PlayerCount < room.MaxPlayers)
                {
                    return room;
                }
            }

            // 如果没有空闲房间，创建一个新的
            var newRid = CreateRoom("", PlayersMaxNum);
            return GetRoom(newRid);
        }

        /// <summary>
        /// 设置房间为空闲状态
        /// </summary>
        /// <param name="rid">房间ID</param>
        public void SetIdleRoom(int rid)
        {
            if (_rooms.TryGetValue(rid, out var room) && 
                room.Status == RoomStatus.Waiting && 
                room.PlayerCount < room.MaxPlayers)
            {
                _idle_rooms.Enqueue(rid);
            }
        }

        #endregion

        #region 玩家管理

        /// <summary>
        /// 玩家加入房间
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <param name="playerData">玩家数据</param>
        /// <returns>是否成功</returns>
        public bool PlayerJoinRoom(int rid, PlayerData playerData)
        {
            if (!_rooms.TryGetValue(rid, out var room))
            {
                _logger?.LogWarning($"[RoomManager] Room {rid} not found");
                return false;
            }

            // 检查玩家是否已经在其他房间
            if (_player_room_mapping.TryGetValue(playerData.ID, out var currentRid))
            {
                if (currentRid == rid)
                {
                    _logger?.LogWarning($"[RoomManager] Player {playerData.ID} already in room {rid}");
                    return false;
                }
                
                // 先离开当前房间
                PlayerLeaveRoom(currentRid, playerData.ID);
            }

            // 加入新房间
            if (room.AddPlayer(playerData))
            {
                _player_room_mapping[playerData.ID] = rid;
                _logger?.Log($"[RoomManager] Player {playerData.ID} joined room {rid}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 玩家离开房间
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <param name="userId">玩家ID</param>
        /// <returns>是否成功</returns>
        public bool PlayerLeaveRoom(int rid, string userId)
        {
            if (!_rooms.TryGetValue(rid, out var room))
            {
                return false;
            }

            if (room.RemovePlayer(userId))
            {
                _player_room_mapping.TryRemove(userId, out _);
                
                // 如果房间空了，关闭房间
                if (room.PlayerCount == 0)
                {
                    CloseRoom(rid);
                }
                else
                {
                    // 否则设置为空闲状态
                    SetIdleRoom(rid);
                }
                
                _logger?.Log($"[RoomManager] Player {userId} left room {rid}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取玩家所在房间
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <returns>房间ID，-1表示不在任何房间</returns>
        public int GetPlayerRoom(string userId)
        {
            if (_player_room_mapping.TryGetValue(userId, out var rid))
            {
                return rid;
            }
            return -1;
        }

        /// <summary>
        /// 检查玩家是否在房间中
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <returns>是否在房间中</returns>
        public bool IsPlayerInRoom(string userId)
        {
            return _player_room_mapping.ContainsKey(userId);
        }

        #endregion

        #region 心跳管理

        /// <summary>
        /// 更新心跳
        /// </summary>
        internal void UpdateHeartbeat()
        {
            foreach (var room in _rooms.Values)
            {
                room.CheckHeartbeat();
            }
        }

        /// <summary>
        /// 更新玩家心跳
        /// </summary>
        /// <param name="userId">玩家ID</param>
        public void UpdatePlayerHeartbeat(string userId)
        {
            if (_player_room_mapping.TryGetValue(userId, out var rid))
            {
                if (_rooms.TryGetValue(rid, out var room))
                {
                    room.UpdatePlayerHeartbeat(userId);
                }
            }
        }

        #endregion

        #region 游戏逻辑

        /// <summary>
        /// 处理所有运行中的房间的帧（由 Field 统一调用）
        /// </summary>
        /// <param name="frameId">全局帧ID</param>
        public async System.Threading.Tasks.Task ProcessAllRoomsFrame(int frameId)
        {
            var runningRooms = _rooms.Values.Where(r => r.Status == RoomStatus.Running).ToList();
            
            if (runningRooms.Count == 0)
            {
                return;
            }
            
            // 并行处理所有运行中的房间
            var tasks = runningRooms.Select(room => room.ProcessFrame(frameId));
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        /// <summary>
        /// 开始房间游戏
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <returns>是否成功</returns>
        public async System.Threading.Tasks.Task<bool> StartRoomGame(int rid)
        {
            if (!_rooms.TryGetValue(rid, out var room))
            {
                return false;
            }

            await room.StartGame();
            return true;
        }

        /// <summary>
        /// 停止房间游戏
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <returns>是否成功</returns>
        public bool StopRoomGame(int rid)
        {
            if (!_rooms.TryGetValue(rid, out var room))
            {
                return false;
            }

            room.StopGame();
            return true;
        }

        /// <summary>
        /// 接收玩家输入
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <param name="inputData">输入数据</param>
        public void ReceivePlayerInput(string userId, byte[] inputData)
        {
            if (_player_room_mapping.TryGetValue(userId, out var rid))
            {
                if (_rooms.TryGetValue(rid, out var room))
                {
                    room.ReceivePlayerInput(userId, inputData);
                }
            }
        }

        /// <summary>
        /// 广播消息到房间
        /// </summary>
        /// <param name="rid">房间ID</param>
        /// <param name="data">消息数据</param>
        /// <param name="index">消息索引</param>
        /// <param name="excludeUserId">排除的玩家ID</param>
        /// <returns>发送成功的玩家数量</returns>
        public async System.Threading.Tasks.Task<int> BroadcastToRoom(int rid, byte[] data, int index, string? excludeUserId = null)
        {
            if (!_rooms.TryGetValue(rid, out var room))
            {
                return 0;
            }

            return await room.BroadcastToRoom(data, index, excludeUserId);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 玩家加入房间事件
        /// </summary>
        /// <param name="userId">玩家ID</param>
        private void OnPlayerJoinRoom(string userId)
        {
            _logger?.Log($"[RoomManager] Player {userId} joined a room");
        }

        /// <summary>
        /// 玩家离开房间事件
        /// </summary>
        /// <param name="userId">玩家ID</param>
        private void OnPlayerLeaveRoom(string userId)
        {
            _player_room_mapping.TryRemove(userId, out _);
            _logger?.Log($"[RoomManager] Player {userId} left a room");
        }

        /// <summary>
        /// 游戏开始事件
        /// </summary>
        /// <param name="rid">房间ID</param>
        private void OnGameStart(int rid)
        {
            _logger?.Log($"[RoomManager] Game started in room {rid}");
        }

        /// <summary>
        /// 游戏结束事件
        /// </summary>
        /// <param name="rid">房间ID</param>
        private void OnGameEnd(int rid)
        {
            _logger?.Log($"[RoomManager] Game ended in room {rid}");
            
            // 游戏结束后可以选择关闭房间或重置房间
            // 这里选择保持房间，等待新玩家加入
            SetIdleRoom(rid);
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取房间数量
        /// </summary>
        public int RoomCount => _room_count;

        /// <summary>
        /// 获取在线玩家数量
        /// </summary>
        public int OnlinePlayerCount => _player_room_mapping.Count;

        /// <summary>
        /// 获取运行中的游戏数量
        /// </summary>
        public int RunningGameCount
        {
            get
            {
                return _rooms.Values.Count(r => r.Status == RoomStatus.Running);
            }
        }

        /// <summary>
        /// 获取等待中的房间数量
        /// </summary>
        public int WaitingRoomCount
        {
            get
            {
                return _rooms.Values.Count(r => r.Status == RoomStatus.Waiting);
            }
        }

        #endregion
    }
}