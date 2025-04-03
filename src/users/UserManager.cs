using AMToolkits.Utility;
using Logger;
using Microsoft.AspNetCore.Builder;

namespace Server
{
    public partial class UserManager : SingletonT<UserManager>, ISingleton
    {
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public UserManager()
        {

        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[UserManager] Config is NULL.");
                return ;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[UserManager] Register Handlers");

            args.app?.Map("api/user/auth", HandleUserAuth);

        }
    }
}