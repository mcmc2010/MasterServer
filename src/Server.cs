
using System.Linq;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using AMToolkits.Utility;
using AMToolkits.Extensions;
using Logger;

#if LINUX
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Server
{
    public class HandlerEventArgs : EventArgs
    {
        public WebApplication? app;
    }

#if LINUX
    public class ServiceWorker : BackgroundService
    {
        private readonly ILogger<ServiceWorker> _logger;
        private CancellationTokenSource? _cts = null;

        public ServiceWorker(ILogger<ServiceWorker> logger)
        {
            _logger = logger;

            this._cts = new CancellationTokenSource();
            // 注册终止信号处理
            Console.CancelKeyPress += OnCancelExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _logger.LogInformation("Service Starting");

            if(this._cts != null) 
            {
                while (!this._cts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, this._cts.Token);
                    }
                    catch(TaskCanceledException)
                    {
                        break;
                    }
                    finally
                    {

                    }
                }
            }

            //
            await Cleanup();

            _logger.LogInformation("Service Ended");
        }

        private async Task Cleanup()
        {
            Console.CancelKeyPress -= OnCancelExit;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            await Task.Delay(100);
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            this._cts?.Cancel();
        }

        //
        private void OnCancelExit(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // 阻止直接退出
            this._cts?.Cancel();
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication : SingletonT<ServerApplication>, ISingleton
    {
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private LoggerEntry? _logger = null;

        private WebApplication? _webserver = null;

        private CancellationTokenSource? _cts = null;

        /// <summary>
        /// 
        /// </summary>
        public event System.EventHandler<HandlerEventArgs>? RegisterHandlersListner = null;

        public ServerApplication()
        {
        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return ;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;

            //
        }

#if LINUX
        public async Task<int> ProcessServiceWorking()
        {
            IHost host = Host.CreateDefaultBuilder(this._arguments)
                .UseSystemd()
                .ConfigureServices(s => {
                    s.AddHostedService<ServiceWorker>();
                })
                .ConfigureLogging(logging => {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .Build();
            await host.RunAsync();
            return 0;
        }
#endif

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
                            listen.UseHttps(v.Certificates.Trim(), null); // 配置 HTTPS
                        });
                    }
                    else
                    {
                        options.Listen(address, v.Port);   // 监听 HTTP 5000 端口
                    }
                }
            });

            //
            var cfg = _config.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "http");
            if(cfg != null && cfg.File.Length > 0) {
                // 添加文件日志提供程序，并设置日志路径和级别
                //builder.Logging.AddFile("logs/main.log", minimumLevel: LogLevel.Information);
                builder.Logging.SetMinimumLevel((Microsoft.Extensions.Logging.LogLevel)cfg.Getlevel());
                // 注册自定义文件日志提供程序
                builder.Logging.AddProvider(new Logger.Extensions.FileLoggerProvider(cfg.File));
            }

            // 
            _webserver = builder.Build();

            //
            RegisterHandlers();
     
            // 捕获所有未匹配的路由，返回默认 JSON
            _webserver.MapFallback(async context =>
            {
                await context.ResponseStatusAsync("error", "Not Found", HttpStatusCode.NotFound);
            });


            //
            if(RegisterHandlersListner != null)
            {
                RegisterHandlersListner(this, new HandlerEventArgs() { app = _webserver });
            }

            //
            _logger?.Log("[Server] Starting HTTPServer");
            return true;
        }

        protected virtual void RegisterHandlers()
        {
            if(_webserver == null) {
                System.Console.WriteLine("[Server] WebServer not initialize.");
                return;
            }

            _webserver.Map("/", HandleHello);
            _webserver.Map("/api/ping", HandlePing);
        }

        public Task<int> StartWorking()
        {
            if(_webserver != null)
            {
                _webserver.RunAsync();
            }

            //
#if LINUX && LINUX_SERVICE
            return this.ProcessServiceWorking();
#else
            return this.ProcessWorking();
#endif  
        }

        private async Task<int> ProcessWorking()
        {
            _logger?.Log("[Server] Start Working");

            this._cts = new CancellationTokenSource();
            // 注册终止信号处理
            Console.CancelKeyPress += OnCancelExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            while (!this._cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, this._cts.Token);
                }
                catch(TaskCanceledException)
                {
                    break;
                }
                finally
                {

                }
            }

            _logger?.Log("[Server] End Working");
            Console.CancelKeyPress -= OnCancelExit;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            await Task.Delay(100);
            return 0;
        }

        public void EndWorking()
        {
            if(_webserver != null)
            {
                _webserver.StopAsync();
            }
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            this._cts?.Cancel();
        }

        //
        private void OnCancelExit(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // 阻止直接退出
            this._cts?.Cancel();
        }
    }
}