using AMToolkits.Utility;
using Logger;
using Microsoft.AspNetCore.Builder;


namespace Server {

    /// <summary>
    /// 
    /// </summary>
    public enum GameMatchTeam
    {
        None = 0,
        Blue = 1,
        Red = 2,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum GameMatchRoomRole
    {
        None = 0,
        Member = 1,
        Master = 7,
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GameMatchManager : AMToolkits.SingletonT<GameMatchManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static GameMatchManager? _instance;


        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private readonly System.Random _rand = new System.Random();
        protected int _aiplayer_count = 0;

        private TaskCompletionSource? _cts_queues = null;

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[MatchManager] Config is NULL.");
                return ;
            }

            _config = config;

            this.InitLogger();
        }
        private void InitLogger()
        {
            //
            _logger = null;
            var cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "game_match");
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

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[MatchManager] Register Handlers");

            args.app?.MapPost("api/game/match", HandleMatchStart);
            args.app?.MapPost("api/game/match_cancel", HandleMatchCancel);
            args.app?.MapPost("api/game/match_completed", HandleMatchCompleted);
        }

        public int StartWorking()
        {
            this.ProcessWorking();
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<int> ProcessWorking()
        {
            if(_config?.MatchServer.Enabled == false)
            {
                _logger?.Log("[MatchServer] Not Enabled");
                return 0;
            }
            
            _logger?.Log("[MatchServer] Start Working");
            
            // 获取当前AI数量
            _aiplayer_count = this.DBAIPlayerCount();
            this.DBAIPlayerInit();

            // 做一次队列超时工作
            DBQueuesTimeout();

            //
            while(!ServerApplication.Instance.HasQuiting)
            {
                int code = await this.QueuesWorking();
                if(code == 0)
                {
                    await ProcessWaitWorking();
                }
                else
                {
                    await Task.Delay(1*1000);
                }
            }
            return 0;
        }

        private async Task<int> TryQueuesWorking()
        {
            if(_cts_queues == null) {
                return 0;
            }
            _cts_queues.TrySetResult();
            return 1;
        }

        private async Task<int> ProcessWaitWorking()
        {
            try
            {
                _cts_queues = new TaskCompletionSource();
                var timeout_task = Task.Delay(30 * 1000);
                var completed_task = await Task.WhenAny(_cts_queues.Task, timeout_task);
                if(completed_task == timeout_task) {
                    _cts_queues.TrySetCanceled();

                    DBQueuesTimeout();
                }

                System.Console.WriteLine($"(Queues) Working (Completed:{_cts_queues.Task.IsCompletedSuccessfully})");
                return 0;
            } catch (Exception e) {
                _logger?.LogException("Error:", e);
                return -1;
            } finally {
                _cts_queues?.Task.Dispose();
                _cts_queues = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async Task<int> QueuesWorking()
        {
            List<GameMatchQueueItem> queue_players = new List<GameMatchQueueItem>();
            // 从数据库中获取可以匹配的玩家
            int result = DBQueues(queue_players);
            if(result <= 0 || queue_players.Count == 0) 
            {
                return 0;
            }

            System.Console.WriteLine($"{TAGName} (Queue) Count:{queue_players.Count}");

            // 匹配AI
            if(_config?.MatchServer.IsAIPlayerEnabled == true)
            {
                if(await this.DBMatchWithAIProcess(queue_players) <= 0)
                {
                    return 0;
                }
            }
            return result;
        }

        protected GameMatchQueueItem NewQueueItem(string id, Dictionary<string, DatabaseResultItem> data, 
                        GameMatchType type = GameMatchType.Normal, GameMatchTeam team = GameMatchTeam.Blue)
        {
            var item = new GameMatchQueueItem()
            {
                sn = id,
                server_id = data["server_id"]?.String ?? "",
                tid = (int)(data["tid"]?.Number ?? 100),
                name = data["name"]?.String ?? "",
                hol_value = (int)(data["hol_value"]?.Number ?? 100),
                type = type,
                team = team,
                role = GameMatchRoomRole.None,
                level = (int)(data["level"]?.Number ?? 0),
                room_id = 0,
                //create_time = DateTime.Now,
                //last_time = DateTime.Now
            };

            // 可选项
            DatabaseResultItem? value;
            if (data.TryGetValue("room_id", out value))
            {
                item.room_id = (int)value.Number;
            }

            return item;
        }

        public int AddAIPlayer(int level = 0)
        {
            // 未开启
            if(_config?.MatchServer.IsAIPlayerEnabled == false)
            {
                return 0;
            }

            // 获取当前AI数量
            _aiplayer_count = this.DBAIPlayerCount();
            // 如果当前AI数量已经达到设置，就不可以再创建
            if(_aiplayer_count >= _config?.MatchServer.AIPlayerMaxNum)
            {
                return 0;
            }

            AIPlayerTemplateData? new_template_data = null;
            // AI 不可以重复
            if(_config?.MatchServer.IsAIPlayerDerived == false)
            {
                List<AIPlayerData> ai_players = new List<AIPlayerData>();
                // 从数据库中获取已经存在的AI
                if(this.DBAIPlayerData(ai_players) <= 0)
                {
                    return -1;
                }
                
                // 随机一个AI
                new_template_data = AIPlayerManager.Instance.Rand(ai_players, -1);
            }
            else
            {
                List<AIPlayerTemplateData> without = new List<AIPlayerTemplateData>();
                // 随机一个AI
                new_template_data = AIPlayerManager.Instance.Rand(without, -1);
            }

            if(new_template_data != null)
            {
                var ai_player_data = AIPlayerManager.Instance.CreatePlayerData(new_template_data);
                if(this.DBAIPlayerDataAdd(ai_player_data) < 0)
                {
                    return -1;
                }
            }

            return 0;
        }
    }
}