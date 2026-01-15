
using System.Net;


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Logger;

using AMToolkits.Utility;
using AMToolkits.Extensions;

////
namespace Server
{
    [System.Serializable]
    public class SessionAuthData
    {
        public string id = "";
        public int result = 0;

        public string token = "";
        public string passphrase = "";
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication
    {
        #region HTTPServer
        //
        public bool CreateHTTPServer()
        {
            if (_config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return false;
            }

            _webservices.Clear();

            //
            var builder = WebApplication.CreateBuilder();

            // 添加服务配置
            // 配置 Kestrel HTTPS
            builder.WebHost.ConfigureKestrel(options =>
            {

                foreach (var v in _config.HTTPServer)
                {
                    IPAddress address = IPAddress.Any;
                    if (v.Address.Trim() != "0.0.0.0")
                    {
                        address = IPAddress.Parse(v.Address);
                    }
                    // HTTPS
                    if (v.HasSSL && v.SSLCertificates.Trim().Length > 0)
                    {
                        var cert = this.LoadCertificate(v.SSLCertificates, v.SSLKey);
                        if (cert == null)
                        {
                            _logger?.LogError($"[Server] (HTTPS) Not Listen {v.Port}, Certificat Error.");
                        }
                        else
                        {
                            options.Listen(address, v.Port, listen =>
                            {
                                // 配置 HTTPS
                                listen.UseHttps(cert);
                                // 添加服务标识中间件
                                ServiceData service;
                                _webservices.Add((System.Net.IPEndPoint)listen.EndPoint, service = new ServiceData()
                                { 
                                    EndPoint = listen.EndPoint.ToString() ?? "",
                                    IsInternalService = v.IsInternalService,
                                });
                                if (v.IsInternalService)
                                {
                                    service.AllowAddressList = new List<string>(v.AllowAddressList);
                                }

                                _logger?.Log($"[Server] (HTTPS) Listen {listen.EndPoint.ToString()} (Certificate SerialNumber: {cert.GetSerialNumberString()})");
                            });
                        }
                    }
                    else
                    {
                        options.Listen(address, v.Port, listen =>
                        {
                            // 添加服务标识中间件
                            ServiceData service;
                            _webservices.Add((System.Net.IPEndPoint)listen.EndPoint, service = new ServiceData()
                            { 
                                EndPoint = listen.EndPoint.ToString() ?? "",
                                IsInternalService = v.IsInternalService,
                            });

                            if (v.IsInternalService)
                            {
                                service.AllowAddressList = new List<string>(v.AllowAddressList);
                            }

                            _logger?.Log($"[Server] (HTTP) Listen {listen.EndPoint.ToString()}");
                        });
                    }
                }
            });

            // 默认忽略的，只有在Debug模式才会显示
            var ignore_endpoints = new List<string>()
            {
                //
                "/favicon.ico",
                //
                "/ping",
                // API
                "/api/ping"
            };

            //
            var cfg = _config.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "http");
            if (cfg != null && cfg.File.Length > 0)
            {
                builder.Logging.ClearProviders();
                // 添加文件日志提供程序，并设置日志路径和级别
                //builder.Logging.AddFile("logs/main.log", minimumLevel: LogLevel.Information);
                builder.Logging.SetMinimumLevel((Microsoft.Extensions.Logging.LogLevel)cfg.Getlevel());
                // 注册自定义文件日志提供程序
                builder.Logging.AddProvider(new Logger.Extensions.LoggerProvider(cfg.File,
                        cfg.Getlevel(), ignore_endpoints));
            }

            // 
            _webserver = builder.Build();

            // 添加请求端IP地址
            _webserver.UseMiddleware<HTTPLoggingMiddleware>();

            //
            RegisterHandlers();

            // 限制IP权限,实现域访问
            _webserver.UseMiddleware<HTTPPrivateDomainMiddleware>(this);


            //
            if (RegisterHandlersListner != null)
            {
                RegisterHandlersListner(this, new HandlerEventArgs() { app = _webserver });
            }

            // 捕获所有未匹配的路由，返回默认 JSON
            _webserver.MapFallback(async context =>
            {
                // 检查响应是否已经被其他中间件处理过
                if (context.Response.HasStarted)
                {
                    // 如果响应已开始或状态码不是200，说明前面已经处理了
                    _logger?.LogDebug($"Fallback skipped - Response already handled. Status: {context.Response.StatusCode}, Started: {context.Response.HasStarted}");
                    return; // 响应已开始，不做任何处理
                }

                // 检查是否已经有终结点匹配（除了fallback）
                var endpoint = context.GetEndpoint();
                if (endpoint != null && endpoint.DisplayName?.Contains("Fallback") != true)
                {
                    _logger?.LogDebug($"Run middleware skipped - Endpoint matched: {endpoint.DisplayName}");
                    return;
                }

                //
                await context.ResponseStatusAsync("error", "Not Found", HttpStatusCode.NotFound);
                await context.Response.CompleteAsync();
            });

            //
            _logger?.Log("[Server] Starting HTTPServer");
            return true;
        }


        protected virtual void RegisterHandlers()
        {
            if (_webserver == null)
            {
                System.Console.WriteLine("[Server] WebServer not initialize.");
                return;
            }

            _webserver.Map("/", HandleHello);
            _webserver.Map("/api/ping", HandlePing);
            _webserver.Map("/local/status", HandleServiceStatus);
        }

        #endregion

        #region 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int CheckSecretKey(HttpContext context)
        {
            var headers = context.Request.Headers;
            if (!headers.TryGetValue("X-SecretKey", out var value))
            {
                return -1;
            }

            bool is_root_key = false;
            string key = value.ToString().Trim().ToUpper();
            if (key.StartsWith("ROOT"))
            {
                is_root_key = true;

                string[] values = key.Split(":");
                if (values.Length > 1)
                {
                    key = values[1];
                }
            }

            if (key.Length != 16 && key.Length != 32 && key.Length != 64)
            {
                return -2;
            }

            if (is_root_key && key != ServerConfigLoader.Config.SecretKey)
            {
                return -7;
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int CheckLoginSession(HttpContext context, SessionAuthData? auth_data = null)
        {
            if (auth_data == null)
            {
                auth_data = new SessionAuthData()
                {
                    result = -1
                };
            }

            var headers = context.Request.Headers;
            if (!headers.TryGetValue("X-Authorization", out var value))
            {
                return -1;
            }
            string text = value.ToString().Trim();

            string key = "";
            string token = "";
            string hash = "";
            string[] values = text.Split(":");
            if (values.Length > 0)
            {
                key = values[0].Trim();
            }
            if (values.Length > 1)
            {
                token = values[1].Trim();
            }
            if (values.Length > 2)
            {
                hash = values[2].Trim();
            }

            auth_data.result = 0;

            // 登陆验证
            int result_code = 0;
            if (_config?.JWTEnabled == true && _config?.JWTAuthorizationEnabled == true
                && (result_code = JWTAuth.JWTVerifyData(hash, _config?.JWTSecretKey ?? "")) <= 0)
            {
                auth_data.result = result_code;
                return 0;
            }

            // DB 验证
            if (_config?.DBAuthorizationEnabled == true
                && (result_code = DBAuthenticationSession(key, token)) <= 0)
            {
                auth_data.result = result_code;
                return 0;
            }

            auth_data.id = key;
            auth_data.result = 1;
            var user = UserManager.Instance.GetUserT<UserBase>(key);
            if (user != null)
            {
                auth_data.token = user.AccessToken;
                auth_data.passphrase = user.Passphrase;
            }

            if (user?.AccessToken.ToUpper() != token.ToUpper())
            {
                _logger?.LogError($"(User) Auth User:{user?.ID}, Token:{user?.AccessToken} - {token} Error");
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="auth_data"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> AuthSessionAndResult(HttpContext context, SessionAuthData? auth_data = null)
        {
            int result = 0;
            if ((result = ServerApplication.Instance.CheckSecretKey(context)) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return result;
            }

            if ((result = ServerApplication.Instance.CheckLoginSession(context, auth_data)) <= 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotLogin);
                return result;
            }
            return result;
        }

        #endregion
    }
}
