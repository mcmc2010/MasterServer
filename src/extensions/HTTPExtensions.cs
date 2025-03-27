using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;


namespace AMToolkits.Extensions
{
    public static class HTTPExtensions
    {
        public static bool IsPrivateOrReserved(this IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            
            // IPv4保留地址检查
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return bytes[0] switch
                {
                    10 => true,                            // 10.0.0.0/8
                    127 => true,                           // 127.0.0.0/8
                    169 when bytes[1] == 254 => true,      // 169.254.0.0/16
                    172 when bytes[1] >= 16 
                            && bytes[1] <= 31 => true,    // 172.16.0.0/12
                    192 when bytes[1] == 168 => true,     // 192.168.0.0/16
                    _ => false
                };
            }

            // IPv6保留地址检查
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                    return true;

                // ::1/128
                if (address.Equals(IPAddress.IPv6Loopback))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IPAddress GetClientAddress(this HttpContext context)
        {
            IPAddress? address = null;

            var headers = context.Request.Headers;
            // 优先从自定义头部获取
            if (headers.TryGetValue("X-Real-IP", out var real_ip))
            {
                IPAddress? a = null;
                if (IPAddress.TryParse(real_ip, out a)) { 
                    address = a;
                }
            }

            if (headers.TryGetValue("X-Forwarded-For", out var forwarded_ips))
            {
                IPAddress? a = null;

                // 分割多个IP并逆序处理（根据代理链顺序）
                var candidates = forwarded_ips.ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Reverse()
                            .Select(ip => ip.Trim());

                foreach (var ip in candidates)
                {
                    // 验证IP格式
                    if (!IPAddress.TryParse(ip, out a))
                        continue;

                    // 过滤保留地址
                    if (a.IsPrivateOrReserved())
                        continue;
                    
                    address = a;
                }

            }

            if(address == null)
            {
                address = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
            }

            return address;
        }


        public static void QueryString(this HttpContext context, string key, out string value, string defval = "")
        {
            value = defval;

            string[] values;
            QueryString(context, key, out values);
            if(values.Length > 0)
            {
                value = values[0];
            }
        }

        public static void QueryString(this HttpContext context, string key, out string[] values)
        {
            values = new string[] { };

            Microsoft.Extensions.Primitives.StringValues vs;
            // 获取timestamp参数并转换为整数
            if (!context.Request.Query.TryGetValue("timestamp", out vs))
            {
            }

            // 处理多值参数的情况
            if (vs.Count > 0)
            {
                values = vs.ToArray<string>();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="content"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Task ResponseJsonAsync(this HttpContext context, object content, HttpStatusCode code = HttpStatusCode.OK)
        {
            context.Response.StatusCode = (int)code;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(content);
        }

        public async static Task ResponseError(this HttpContext context, HttpStatusCode code = HttpStatusCode.BadRequest, string message = "")
        {
            object? data = null;
            if(message.Length == 0) {
                message = "Bad Request";
            }

            var result = new {
                Code = code,
                Status = "error",
                Message = message,
                Data = data
            };
            await context.ResponseJsonAsync(result, code);
        }

        
        public async static Task ResponseResult(this HttpContext context, object? data = null, HttpStatusCode code = HttpStatusCode.OK)
        {
            var result = new {
                Code = code,
                Status = "success",
                Data = data
            };
            await context.ResponseJsonAsync(result, code);
        }
    }
}