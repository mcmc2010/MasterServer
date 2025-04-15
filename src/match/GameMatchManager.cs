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


        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

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

        public Task<int> StartWorking()
        {
            return this.ProcessWorking();
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

            //
            this.QueuesWorking();
            return 0;
        }

        private async void QueuesWorking()
        {
            DBQueues();
        }
    }
}