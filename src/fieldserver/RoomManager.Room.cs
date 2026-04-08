using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Logger;
using Server.Services;


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
    /// 玩家输入数据
    /// </summary>
    public class PlayerInput
    {
        public string UserId { get; set; } = "";
        public int FrameId { get; set; }
        public byte[] InputData { get; set; } = Array.Empty<byte>();
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// 帧数据
    /// </summary>
    public class FrameData
    {
        public int FrameId { get; set; }
        public long Timestamp { get; set; }
        public Dictionary<string, PlayerInput> Inputs { get; set; } = new();
        public byte[] StateData { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// 游戏状态
    /// </summary>
    public class GameState
    {
        public int Tick { get; set; } = 0;
        public Dictionary<string, PlayerState> Players { get; set; } = new();
        public List<GameObject> Objects { get; set; } = new();
        public Dictionary<string, object> GameData { get; set; } = new();
    }

    /// <summary>
    /// 玩家状态
    /// </summary>
    public class PlayerState
    {
        public string UserId { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Rotation { get; set; }
        public int Health { get; set; } = 100;
        public int Score { get; set; } = 0;
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    /// <summary>
    /// 游戏对象
    /// </summary>
    public class GameObject
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 房间配置
    /// </summary>
    public class RoomConfig
    {
        public int TickRate { get; set; } = 30;          // Tick率（次/秒）
        public int MaxPlayers { get; set; } = 10;        // 最大玩家数
        public bool AllowLateJoin { get; set; } = true;  // 允许中途加入
        public int InputBufferSize { get; set; } = 3;    // 输入缓冲区大小
        public int HeartbeatTimeout { get; set; } = 60;  // 心跳超时（秒）
    }

    /// <summary>
    /// 房间 - 基于Nakama帧同步机制
    /// </summary>
    public class Room : IDisposable
    {
        private bool _is_disposed = false;
        private readonly Logger.LoggerEntry? _logger = Logger.LoggerFactory.Instance;

        // 帧同步相关
        private long _frame_interval = 33;  // 帧间隔（毫秒）
        private int _current_frame = 0;     // 当前帧ID
        private long _last_tick_time = 0;   // 上次Tick时间
        private bool _is_running = false;   // 是否正在运行
        private readonly object _frame_lock = new object();

        // 输入缓冲区
        private readonly ConcurrentDictionary<int, Dictionary<string, PlayerInput>> _input_buffer = new();

        // 游戏状态
        private GameState _game_state = new GameState();

        // 房间配置
        private RoomConfig _config = new RoomConfig();

        // 房间状态
        private Field.RoomStatus _status = Field.RoomStatus.None;

        // 玩家列表
        private readonly ConcurrentDictionary<string, Field.PlayerData> _players;

        // 心跳时间记录
        private readonly ConcurrentDictionary<string, long> _player_heartbeats = new();

        // 房间信息
        private readonly int _room_id;
        private readonly string _room_name;
        private readonly int _players_maxnum;

        // 事件
        public event Action<int, FrameData>? OnFrameUpdate;      // 帧更新事件
        public event Action<string, PlayerInput>? OnInputReceived; // 输入接收事件
        public event Action<string>? OnPlayerJoin;                // 玩家加入事件
        public event Action<string>? OnPlayerLeave;               // 玩家离开事件
        public event Action? OnGameStart;                         // 游戏开始事件
        public event Action? OnGameEnd;                           // 游戏结束事件

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">房间ID</param>
        /// <param name="name">房间名称</param>
        /// <param name="players_maxnum">最大玩家数</param>
        public Room(int id, string name, int players_maxnum = 20)
        {
            _room_id = id;
            _room_name = name;
            _players_maxnum = players_maxnum;

            // 计算帧间隔
            _frame_interval = (long)(1000.0f / _config.TickRate);

            // 初始化玩家列表
            _players = new ConcurrentDictionary<string, PlayerData>();

            // 初始化游戏状态
            InitializeGameState();

            _logger?.Log($"[Room] Room {_room_id} created with tick rate {_config.TickRate}");
        }

        /// <summary>
        /// 初始化游戏状态
        /// </summary>
        private void InitializeGameState()
        {
            _game_state = new GameState
            {
                Tick = 0,
                Players = new Dictionary<string, PlayerState>(),
                Objects = new List<GameObject>(),
                GameData = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_is_disposed)
            {
                return;
            }

            _is_disposed = true;
            _is_running = false;

            // 清理事件
            OnFrameUpdate = null;
            OnInputReceived = null;
            OnPlayerJoin = null;
            OnPlayerLeave = null;
            OnGameStart = null;
            OnGameEnd = null;

            _logger?.Log($"[Room] Room {_room_id} disposed");
        }

        #region 玩家管理

        /// <summary>
        /// 添加玩家到房间
        /// </summary>
        /// <param name="playerData">玩家数据</param>
        /// <returns>是否成功</returns>
        public bool AddPlayer(PlayerData playerData)
        {
            if (_players.Count >= _players_maxnum)
            {
                _logger?.LogWarning($"[Room] Room {_room_id} is full, cannot add player {playerData.ID}");
                return false;
            }

            if (_players.ContainsKey(playerData.ID))
            {
                _logger?.LogWarning($"[Room] Player {playerData.ID} already in room {_room_id}");
                return false;
            }

            // 添加玩家到列表
            _players[playerData.ID] = playerData;
            _player_heartbeats[playerData.ID] = AMToolkits.Utils.GetLongTimestamp();

            // 添加玩家到游戏状态
            _game_state.Players[playerData.ID] = new PlayerState
            {
                UserId = playerData.ID,
                PositionX = 0,
                PositionY = 0,
                PositionZ = 0,
                Rotation = 0,
                Health = 100,
                Score = 0
            };

            _logger?.Log($"[Room] Player {playerData.ID} joined room {_room_id}");

            // 触发玩家加入事件
            OnPlayerJoin?.Invoke(playerData.ID);

            // 如果房间等待中且玩家数足够，自动准备
            if (_status == RoomStatus.Waiting && _players.Count >= 2)
            {
                SetStatus(RoomStatus.Ready);
            }

            return true;
        }

        /// <summary>
        /// 从房间移除玩家
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <returns>是否成功</returns>
        public bool RemovePlayer(string userId)
        {
            if (!_players.TryRemove(userId, out _))
            {
                return false;
            }

            _player_heartbeats.TryRemove(userId, out _);

            // 从游戏状态中移除玩家
            _game_state.Players.Remove(userId);

            // 清理该玩家的输入缓冲区
            foreach (var frameInputs in _input_buffer.Values)
            {
                frameInputs.Remove(userId);
            }

            _logger?.Log($"[Room] Player {userId} left room {_room_id}");

            // 触发玩家离开事件
            OnPlayerLeave?.Invoke(userId);

            // 如果房间空了，结束游戏
            if (_players.Count == 0 && _status == RoomStatus.Running)
            {
                StopGame();
            }

            return true;
        }

        /// <summary>
        /// 移除所有玩家
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task RemoveAllPlayer()
        {
            var playerIds = _players.Keys.ToList();
            foreach (var playerId in playerIds)
            {
                RemovePlayer(playerId);
            }

            await System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <returns>玩家数据</returns>
        public PlayerData? GetPlayer(string userId)
        {
            _players.TryGetValue(userId, out var player);
            return player;
        }

        /// <summary>
        /// 获取所有玩家
        /// </summary>
        /// <returns>玩家列表</returns>
        public List<PlayerData> GetAllPlayers()
        {
            return _players.Values.ToList();
        }

        /// <summary>
        /// 获取玩家数量
        /// </summary>
        public int PlayerCount => _players.Count;

        /// <summary>
        /// 检查玩家是否在房间中
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <returns>是否在房间中</returns>
        public bool HasPlayer(string userId)
        {
            return _players.ContainsKey(userId);
        }

        #endregion

        #region 心跳管理

        /// <summary>
        /// 更新玩家心跳
        /// </summary>
        /// <param name="userId">玩家ID</param>
        public void UpdatePlayerHeartbeat(string userId)
        {
            if (_players.ContainsKey(userId))
            {
                _player_heartbeats[userId] = AMToolkits.Utils.GetLongTimestamp();
            }
        }

        /// <summary>
        /// 检查心跳超时
        /// </summary>
        public void CheckHeartbeat()
        {
            if (_is_disposed || _players.Count == 0)
            {
                return;
            }

            var currentTime = AMToolkits.Utils.GetLongTimestamp();
            var timeoutThreshold = _config.HeartbeatTimeout * 1000; // 转换为毫秒

            var timeoutPlayers = new List<string>();

            foreach (var kvp in _player_heartbeats)
            {
                if (currentTime - kvp.Value > timeoutThreshold)
                {
                    timeoutPlayers.Add(kvp.Key);
                }
            }

            foreach (var playerId in timeoutPlayers)
            {
                _logger?.LogWarning($"[Room] Player {playerId} heartbeat timeout in room {_room_id}");
                RemovePlayer(playerId);
            }
        }

        #endregion

        #region 帧同步核心

        /// <summary>
        /// 启动帧循环
        /// </summary>
        public async System.Threading.Tasks.Task StartFrameLoop()
        {
            if (_is_running)
            {
                return;
            }

            _is_running = true;
            _last_tick_time = AMToolkits.Utils.GetLongTimestamp();
            _current_frame = 0;

            _logger?.Log($"[Room] Room {_room_id} frame loop started");

            while (_is_running && _status == RoomStatus.Running && !_is_disposed)
            {
                var currentTime = AMToolkits.Utils.GetLongTimestamp();
                var elapsed = currentTime - _last_tick_time;

                // 检查是否到达下一Tick
                if (elapsed >= _frame_interval)
                {
                    // 处理当前帧
                    await ProcessFrame(_current_frame);

                    // 更新帧ID和时间
                    _current_frame++;
                    _last_tick_time = currentTime;
                }

                // 短暂休眠，避免CPU占用过高
                await System.Threading.Tasks.Task.Delay(1);
            }

            _logger?.Log($"[Room] Room {_room_id} frame loop stopped");
        }

        /// <summary>
        /// 停止帧循环
        /// </summary>
        public void StopFrameLoop()
        {
            _is_running = false;
        }

        /// <summary>
        /// 处理单帧
        /// </summary>
        /// <param name="frameId">帧ID</param>
        private async System.Threading.Tasks.Task ProcessFrame(int frameId)
        {
            try
            {
                // 1. 收集当前帧的输入
                var frameInputs = CollectFrameInputs(frameId);

                // 2. 验证输入
                var validatedInputs = ValidateInputs(frameInputs);

                // 3. 更新游戏状态
                UpdateGameState(validatedInputs);

                // 4. 创建帧数据
                var frameData = new FrameData
                {
                    FrameId = frameId,
                    Timestamp = AMToolkits.Utils.GetLongTimestamp(),
                    Inputs = validatedInputs,
                    StateData = SerializeGameState(_game_state)
                };

                // 5. 广播帧数据给所有玩家
                await BroadcastFrameData(frameData);

                // 6. 触发帧更新事件
                OnFrameUpdate?.Invoke(frameId, frameData);

                // 7. 清理旧帧数据
                CleanupOldFrames(frameId);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[Room] Frame {frameId} processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// 收集帧输入
        /// </summary>
        /// <param name="frameId">帧ID</param>
        /// <returns>玩家输入字典</returns>
        private Dictionary<string, PlayerInput> CollectFrameInputs(int frameId)
        {
            var inputs = new Dictionary<string, PlayerInput>();

            // 从输入缓冲区获取当前帧的输入
            if (_input_buffer.TryGetValue(frameId, out var frameInputs))
            {
                foreach (var kvp in frameInputs)
                {
                    inputs[kvp.Key] = kvp.Value;
                }
            }

            // 为没有输入的玩家添加空输入
            foreach (var playerId in _players.Keys)
            {
                if (!inputs.ContainsKey(playerId))
                {
                    inputs[playerId] = new PlayerInput
                    {
                        UserId = playerId,
                        FrameId = frameId,
                        InputData = Array.Empty<byte>(),
                        Timestamp = AMToolkits.Utils.GetLongTimestamp()
                    };
                }
            }

            return inputs;
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        /// <param name="inputs">原始输入</param>
        /// <returns>验证后的输入</returns>
        private Dictionary<string, PlayerInput> ValidateInputs(Dictionary<string, PlayerInput> inputs)
        {
            var validated = new Dictionary<string, PlayerInput>();

            foreach (var kvp in inputs)
            {
                var input = kvp.Value;

                // 验证玩家是否在房间中
                if (!_players.ContainsKey(input.UserId))
                {
                    continue;
                }

                // 验证输入时间戳（防止过期的输入）
                var currentTime = AMToolkits.Utils.GetLongTimestamp();
                if (currentTime - input.Timestamp > 1000) // 超过1秒的输入视为过期
                {
                    continue;
                }

                // 验证输入数据格式
                if (ValidateInputData(input.InputData))
                {
                    validated[input.UserId] = input;
                }
            }

            return validated;
        }

        /// <summary>
        /// 验证输入数据
        /// </summary>
        /// <param name="inputData">输入数据</param>
        /// <returns>是否有效</returns>
        private bool ValidateInputData(byte[] inputData)
        {
            // 这里可以添加具体的输入验证逻辑
            // 例如：检查输入数据长度、格式等
            return inputData != null;
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="inputs">验证后的输入</param>
        private void UpdateGameState(Dictionary<string, PlayerInput> inputs)
        {
            _game_state.Tick = _current_frame;

            foreach (var kvp in inputs)
            {
                var userId = kvp.Key;
                var input = kvp.Value;

                if (_game_state.Players.TryGetValue(userId, out var playerState))
                {
                    // 处理移动输入
                    ProcessMovementInput(playerState, input.InputData);

                    // 处理技能输入
                    ProcessSkillInput(playerState, input.InputData);

                    // 处理其他输入
                    ProcessOtherInput(playerState, input.InputData);
                }
            }

            // 更新游戏对象
            UpdateGameObjects();

            // 检查游戏结束条件
            CheckGameEndCondition();
        }

        /// <summary>
        /// 处理移动输入
        /// </summary>
        /// <param name="playerState">玩家状态</param>
        /// <param name="inputData">输入数据</param>
        private void ProcessMovementInput(PlayerState playerState, byte[] inputData)
        {
            // 解析移动输入（假设前8个字节是移动方向）
            if (inputData.Length >= 8)
            {
                float moveX = BitConverter.ToSingle(inputData, 0);
                float moveZ = BitConverter.ToSingle(inputData, 4);

                // 更新位置
                playerState.PositionX += moveX * 0.1f; // 移动速度
                playerState.PositionZ += moveZ * 0.1f;
            }
        }

        /// <summary>
        /// 处理技能输入
        /// </summary>
        /// <param name="playerState">玩家状态</param>
        /// <param name="inputData">输入数据</param>
        private void ProcessSkillInput(PlayerState playerState, byte[] inputData)
        {
            // 解析技能输入（假设第9个字节是技能ID）
            if (inputData.Length >= 9)
            {
                byte skillId = inputData[8];
                // 处理技能逻辑
            }
        }

        /// <summary>
        /// 处理其他输入
        /// </summary>
        /// <param name="playerState">玩家状态</param>
        /// <param name="inputData">输入数据</param>
        private void ProcessOtherInput(PlayerState playerState, byte[] inputData)
        {
            // 处理其他类型的输入
        }

        /// <summary>
        /// 更新游戏对象
        /// </summary>
        private void UpdateGameObjects()
        {
            // 更新游戏对象的状态
            foreach (var obj in _game_state.Objects)
            {
                // 更新对象位置、状态等
            }
        }

        /// <summary>
        /// 检查游戏结束条件
        /// </summary>
        private void CheckGameEndCondition()
        {
            // 检查游戏是否应该结束
            // 例如：玩家数量不足、时间限制等
            if (_players.Count < 2 && _status == RoomStatus.Running)
            {
                _logger?.Log($"[Room] Room {_room_id} game ending - not enough players");
                StopGame();
            }
        }

        /// <summary>
        /// 序列化游戏状态
        /// </summary>
        /// <param name="gameState">游戏状态</param>
        /// <returns>序列化后的字节数组</returns>
        private byte[] SerializeGameState(GameState gameState)
        {
            // 这里可以使用Protobuf或其他序列化方式
            // 暂时使用简单的JSON序列化
            var json = System.Text.Json.JsonSerializer.Serialize(gameState);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 清理旧帧数据
        /// </summary>
        /// <param name="currentFrameId">当前帧ID</param>
        private void CleanupOldFrames(int currentFrameId)
        {
            // 保留最近几帧的数据
            var framesToRemove = _input_buffer.Keys.Where(k => k < currentFrameId - 10).ToList();
            foreach (var frameId in framesToRemove)
            {
                _input_buffer.TryRemove(frameId, out _);
            }
        }

        /// <summary>
        /// 接收玩家输入
        /// </summary>
        /// <param name="userId">玩家ID</param>
        /// <param name="inputData">输入数据</param>
        public void ReceivePlayerInput(string userId, byte[] inputData)
        {
            if (!_players.ContainsKey(userId))
            {
                return;
            }

            var currentTime = AMToolkits.Utils.GetLongTimestamp();

            // 创建玩家输入
            var playerInput = new PlayerInput
            {
                UserId = userId,
                FrameId = _current_frame + _config.InputBufferSize, // 缓冲区偏移
                InputData = inputData,
                Timestamp = currentTime
            };

            // 添加到输入缓冲区
            var targetFrame = playerInput.FrameId;
            _input_buffer.AddOrUpdate(targetFrame,
                new Dictionary<string, PlayerInput> { { userId, playerInput } },
                (frameId, existing) =>
                {
                    existing[userId] = playerInput;
                    return existing;
                });

            // 触发输入接收事件
            OnInputReceived?.Invoke(userId, playerInput);
        }

        /// <summary>
        /// 广播帧数据
        /// </summary>
        /// <param name="frameData">帧数据</param>
        private async System.Threading.Tasks.Task BroadcastFrameData(FrameData frameData)
        {
            // 创建帧更新消息
            var frameMessage = new Protocols.Field.FrameUpdateMessage
            {
                FrameId = frameData.FrameId,
                Timestamp = frameData.Timestamp,
                StateData = ByteString.CopyFrom(frameData.StateData)
            };

            // 添加玩家输入
            foreach (var kvp in frameData.Inputs)
            {
                frameMessage.Inputs.Add(new Protocols.Field.PlayerInputData
                {
                    UserId = kvp.Key,
                    InputData = ByteString.CopyFrom(kvp.Value.InputData)
                });
            }

            // 广播给房间内所有玩家
            await BroadcastToRoom(frameMessage.ToByteArray(), (int)PacketHandleIndex.FrameUpdate);
        }

        /// <summary>
        /// 广播消息给房间内所有玩家
        /// </summary>
        /// <param name="data">消息数据</param>
        /// <param name="index">消息索引</param>
        /// <param name="excludeUserId">排除的玩家ID</param>
        /// <returns>发送成功的玩家数量</returns>
        public async System.Threading.Tasks.Task<int> BroadcastToRoom(byte[] data, int index, string? excludeUserId = null)
        {
            int count = 0;

            foreach (var player in _players.Values)
            {
                // 排除指定玩家
                if (excludeUserId != null && player.ID == excludeUserId)
                {
                    continue;
                }

                // 获取玩家连接并发送消息
                var service = FieldServer.Instance.GetPlayerService(player.ID);
                if (service != null)
                {
                    await service.BroadcastAsync(data, index);
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region 游戏生命周期

        /// <summary>
        /// 开始游戏
        /// </summary>
        public async System.Threading.Tasks.Task StartGame()
        {
            if (_status != RoomStatus.Ready)
            {
                _logger?.LogWarning($"[Room] Room {_room_id} cannot start - status is {_status}");
                return;
            }

            if (_players.Count < 2)
            {
                _logger?.LogWarning($"[Room] Room {_room_id} cannot start - not enough players");
                return;
            }

            _status = RoomStatus.Running;
            _current_frame = 0;

            // 初始化游戏状态
            InitializeGameState();

            // 广播游戏开始消息
            var startMessage = new Protocols.Field.GameStartMessage
            {
                RoomId = _room_id,
                TickRate = _config.TickRate,
                Timestamp = AMToolkits.Utils.GetLongTimestamp()
            };

            await BroadcastToRoom(startMessage.ToByteArray(), (int)PacketHandleIndex.GameStart);

            _logger?.Log($"[Room] Room {_room_id} game started");

            // 触发游戏开始事件
            OnGameStart?.Invoke();

            // 启动帧循环
            _ = StartFrameLoop();
        }

        /// <summary>
        /// 停止游戏
        /// </summary>
        public async void StopGame()
        {
            if (_status != RoomStatus.Running)
            {
                return;
            }

            _is_running = false;
            _status = RoomStatus.Ended;

            // 广播游戏结束消息
            var endMessage = new Protocols.Field.GameEndMessage
            {
                RoomId = _room_id,
                FinalState = ByteString.CopyFrom(SerializeGameState(_game_state)),
                Timestamp = AMToolkits.Utils.GetLongTimestamp()
            };

            await BroadcastToRoom(endMessage.ToByteArray(), (int)PacketHandleIndex.GameEnd);

            _logger?.Log($"[Room] Room {_room_id} game ended");

            // 触发游戏结束事件
            OnGameEnd?.Invoke();
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (_status == RoomStatus.Running)
            {
                _status = RoomStatus.Paused;
                _is_running = false;
                _logger?.Log($"[Room] Room {_room_id} game paused");
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (_status == RoomStatus.Paused)
            {
                _status = RoomStatus.Running;
                _logger?.Log($"[Room] Room {_room_id} game resumed");
                _ = StartFrameLoop();
            }
        }

        /// <summary>
        /// 设置房间状态
        /// </summary>
        /// <param name="status">新状态</param>
        public void SetStatus(RoomStatus status)
        {
            _status = status;
            _logger?.Log($"[Room] Room {_room_id} status changed to {status}");
        }

        /// <summary>
        /// 获取房间状态
        /// </summary>
        public RoomStatus Status => _status;

        /// <summary>
        /// 获取房间ID
        /// </summary>
        public int RoomId => _room_id;

        /// <summary>
        /// 获取房间名称
        /// </summary>
        public string RoomName => _room_name;

        /// <summary>
        /// 获取最大玩家数
        /// </summary>
        public int MaxPlayers => _players_maxnum;

        /// <summary>
        /// 获取当前帧ID
        /// </summary>
        public int CurrentFrame => _current_frame;

        /// <summary>
        /// 获取游戏状态
        /// </summary>
        public GameState GetGameState()
        {
            return _game_state;
        }

        #endregion
    }
}