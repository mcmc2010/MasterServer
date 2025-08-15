using System.Net;


using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

//
using AMToolkits.Extensions;
using Logger;



namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class HTTPBaseMiddleware
    {
        protected readonly RequestDelegate _next;
        protected readonly Microsoft.Extensions.Logging.ILogger _logger;

        public HTTPBaseMiddleware(RequestDelegate next, ILogger<HTTPLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class HTTPLoggingMiddleware : HTTPBaseMiddleware
    {
        public HTTPLoggingMiddleware(RequestDelegate next, ILogger<HTTPLoggingMiddleware> logger)
                : base(next, logger)
        {

        }


#pragma warning disable CA2017
        public async Task Invoke(HttpContext context)
        {
            var ip = context.GetClientAddress();
            using (var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Scheme"] = context.Request.Scheme.ToUpper(),
                ["Methed"] = context.Request.Method.ToUpper(),
                ["Path"] = context.Request.Path.ToString(),
                ["StatusCode"] = context.Response.StatusCode,
                ["RemoteIP"] = ip,
                ["UserAgent"] = context.Request.Headers.UserAgent
            }))
            {
                await _next(context);
                _logger.LogInformation("[{Scheme}] ({Methed})  Remote Client : {Path} - {StatusCode} [{RemoteIP}]");
            }
        }
#pragma warning restore CA2017
    }

    public class HTTPPrivateDomainMiddleware : HTTPBaseMiddleware
    {
        private readonly Server.ServerApplication _server;
        public HTTPPrivateDomainMiddleware(RequestDelegate next, ILogger<HTTPLoggingMiddleware> logger, Server.ServerApplication server)
                : base(next, logger)
        {
            _server = server;
        }

        public async Task Invoke(HttpContext context)
        {
            var internal_service = _server.CheckInternalService(context.Connection.LocalIpAddress, context.Connection.LocalPort);
            if (internal_service?.IsInternalService == false)
            {
                internal_service = null;
            }

            
            string[] paths = new string[] {
                "/local",
                "/internal",
                "/api/internal",
                "/api/local"
            };

            string[] shared = new string[] {
                "/",
                "/api/ping"
            };

            if (paths.Any(v => context.Request.Path.StartsWithSegments(v, StringComparison.OrdinalIgnoreCase)))
            {
                var ip = context.Connection.RemoteIpAddress;
                // 如果服务不是内部服务，正常处理
                if (ip == null || internal_service == null && !IPAddress.IsLoopback(ip))
                {
                    await context.ResponseStatusAsync("error", "Not Allow Access", HttpStatusCode.Forbidden);
                    return;
                }
                // 判断是否在名单中
                else if (internal_service?.IsInternalService == true &&
                    !(internal_service.AllowAddressList?.Any(v => v == ip.ToString()) == true))
                {
                    await context.ResponseStatusAsync("error", "IP Address Not Allow", HttpStatusCode.Forbidden);
                    return;
                }
            }
            else if (internal_service?.IsInternalService == true &&
                !shared.Any(v => String.Compare(context.Request.Path, v, StringComparison.OrdinalIgnoreCase) == 0))
            {
                await context.ResponseStatusAsync("error", "Not Found", HttpStatusCode.Forbidden);
                return;
            }

            //
            await _next(context);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication
    {

    }

}