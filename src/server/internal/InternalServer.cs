
using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class InternalService : AMToolkits.SingletonT<InternalService>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static InternalService? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public InternalService()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[InternalService] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
            
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.Map("api/internal/user/wallet/data", HandleUserWalletData);
            args.app?.Map("api/internal/user/wallet/update", HandleUserWalletUpdate);

            
            //
            args.app?.Map("api/internal/game/pvp/completed", HandleGamePVPCompleted);

        }
    }
}