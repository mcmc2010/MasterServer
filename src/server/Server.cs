
using System.Linq;
using System.Net;

using Microsoft.AspNetCore.Builder;
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
    public partial class ServerApplication : AMToolkits.SingletonT<ServerApplication>, AMToolkits.ISingleton
    {
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private LoggerEntry? _logger = null;

        private WebApplication? _webserver = null;

        private CancellationTokenSource? _cts = null;
        public bool HasQuiting { get { return _cts?.IsCancellationRequested == true; } }

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
            if (config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return;
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



        public Task<int> StartWorking()
        {
            if (_webserver != null)
            {
                _webserver.RunAsync();
            }

            if (_wsserver != null)
            {
                _wsserver.StartWorking();
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
                catch (TaskCanceledException)
                {
                    break;
                }
                finally
                {

                }
            }

            _wsserver?.Destory();

            _logger?.Log("[Server] End Working");
            Console.CancelKeyPress -= OnCancelExit;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            await Task.Delay(100);
            return 0;
        }

        public void EndWorking()
        {
            if (_webserver != null)
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