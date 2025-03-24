
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication : SingletonT<ServerApplication>, ISingleton
    {
        private ConfigEntry? _config = null;
        private WebApplication? _webserver = null;

        public ServerApplication()
        {
        }

        protected override void OnInitialize(object[] paramters) 
        { 
            var config = paramters[0] as ConfigEntry;
            if(config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return ;
            }
            _config = config;

            //
        }

        public bool CreateHTTPServer()
        {
            if(_config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return false;
            }

            //
            var builder = WebApplication.CreateBuilder();

            // 添加服务配置
            // 配置 Kestrel HTTPS
            builder.WebHost.ConfigureKestrel(options => {

                foreach(var v in _config.HTTPServer)
                {
                    IPAddress address = IPAddress.Any;
                    if(v.Address.Trim() != "0.0.0.0")
                    {
                        address = IPAddress.Parse(v.Address);
                    }
                    if(v.HasSSL && v.Certificates.Trim().Length > 0) {
                        options.Listen(IPAddress.Any, 5443, listen =>
                        {
                            listen.UseHttps(v.Certificates.Trim(), ""); // 配置 HTTPS
                        });
                    }
                    else
                    {
                        options.Listen(address, v.Port);   // 监听 HTTP 5000 端口
                    }
                }
            });

            //
            // 添加文件日志提供程序，并设置日志路径和级别
            //builder.Logging.AddFile("logs/main.log", minimumLevel: LogLevel.Information);
            builder.Logging.SetMinimumLevel(_config.Logging.Getlevel());
            // 注册自定义文件日志提供程序
            builder.Logging.AddProvider(new Logger.FileLoggerProvider(_config.Logging.File));

            // 
            _webserver = builder.Build();

            //
            RegisterHandlers();

            //
            return true;
        }

        protected virtual void RegisterHandlers()
        {
            if(_webserver == null) {
                System.Console.WriteLine("[Server] WebServer not initialize.");
                return;
            }

            _webserver.Map("/", HandleHello);
        }

        public void StartWorking()
        {
            if(_webserver != null)
            {
                _webserver.RunAsync();
            }
        }

        public void EndWorking()
        {
            if(_webserver != null)
            {
                _webserver.StopAsync();
            }
        }
    }
}