using AMToolkits.Utility;
using Logger;
using Microsoft.AspNetCore.Builder;


namespace Server {

    /// <summary>
    /// 
    /// </summary>
    public partial class GameMatchManager : SingletonT<GameMatchManager>, ISingleton
    {
        [AutoInitInstance]
        protected static GameMatchManager? _instance;


        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        protected int _aiplayer_count = 0;

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters);

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
                    await Task.Delay(30 * 1000);
                    DBQueuesTimeout();
                }
                await Task.Delay(5*1000);
            }
            return 0;
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