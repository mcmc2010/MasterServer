
namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServerApplication : SingletonT<ServerApplication>, ISingleton
    {
        private ServerConfig _server_config;

        public ServerApplication()
        {
            _server_config = new ServerConfig();
        }

        protected override void OnInitialize(object[] paramters) 
        { 
            var config = paramters[0] as ServerConfig;
            if(config == null)
            {

                return ;
            }
            this._server_config = config;
        }
    }
}