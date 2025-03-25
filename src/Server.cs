
using System.Net;
using Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#if LINUX
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Server
{
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
    public partial class ServerApplication : Utils.SingletonT<ServerApplication>, Utils.ISingleton
    {
        private string[]? _arguments = null;
        private ConfigEntry? _config = null;

        private WebApplication? _webserver = null;

        private CancellationTokenSource? _cts = null;

        public ServerApplication()
        {
        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

            var config = paramters[1] as ConfigEntry;
            if(config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return ;
            }
            _config = config;

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
            builder.Logging.AddProvider(new Logger.Extensions.FileLoggerProvider(_config.Logging.File));

            // 
            _webserver = builder.Build();

            //
            RegisterHandlers();
            
            //
            Logger.LoggerFactory.Instance?.Log("[Server] Starting HTTPServer.");
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