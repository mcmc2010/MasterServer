
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
                    172 when bytes[1] >= 16 && bytes[1] <= 31 => true,    // 172.16.0.0/12
                    192 when bytes[1] == 168 => true,     // 192.168.0.0/16
                    _ => false
                };
            }

            // IPv6保留地址检查
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                {
                    return true;
                }

                // ::1/128
                if (address.Equals(IPAddress.IPv6Loopback))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 不可以使用同步函数，只能使用异步函数JsonBodyAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        [System.Obsolete("Not using")]
        public static T? JsonBody<T>(this HttpRequest request)
        {
            try
            {
                var reader = new StreamReader(request.Body);
                var json = reader.ReadToEnd();
                request.Body.Position = 0;
                if (json.Length == 0)
                {
                    return default(T);
                }

                var body = System.Text.Json.JsonSerializer.Deserialize<T>(json,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                }
                );
                return body;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return default(T);
            }
        }
        public static async Task<T?> JsonBodyAsync<T>(this HttpRequest request)
        {
            try
            {
                var reader = new StreamReader(request.Body);
                var json = await reader.ReadToEndAsync();
                if (json.Length == 0)
                {
                    return default(T);
                }

                var body = System.Text.Json.JsonSerializer.Deserialize<T>(json,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                }
                );
                return body;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return default(T);
            }
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
                if (IPAddress.TryParse(real_ip, out a))
                {
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

            if (address == null)
            {
                address = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
            }

            return address;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static System.Security.Cryptography.X509Certificates.X509Certificate2? GetClientCertificate(this HttpContext context)
        {
            try
            {
                // 方法1：从连接获取
                var certificate = context.Connection.ClientCertificate;
                if (certificate != null && certificate.Thumbprint != null)
                {
                    return certificate;
                }

                // 方法3：从自定义头获取
                if (context.Request.Headers.TryGetValue("X-Client-Certificate", out var pem))
                {
                    // 移除PEM格式的头部和尾部
                    var b64 = pem.ToString()
                        .Replace("-----BEGIN CERTIFICATE-----", "")
                        .Replace("-----END CERTIFICATE-----", "")
                        .Replace("\n", "");

                    var data = Convert.FromBase64String(b64);
                    certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(data);
                }

                return null;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetOSPlatform(this HttpContext context)
        {
            string desc = "";
            return GetOSPlatform(context, out desc);
        }

        public static string GetOSPlatform(this HttpContext context, out string os_desc)
        {
            os_desc = "";

            string value = "";
            context.QueryString("platform", out value);
            var vs = value.Split("_");
            if (vs.Length > 0)
            {
                value = vs[0];
            }
            if (vs.Length > 1 && System.Text.RegularExpressions.Regex.IsMatch(vs[1], @"^\d[\d.]*$"))
            {
                os_desc = vs[1];
            }

            var platform = "Unknow";
            if (string.Compare(value, AMToolkits.OSPlatform.Window, true) == 0)
            {
                platform = AMToolkits.OSPlatform.Window;
            }
            else if (string.Compare(value, AMToolkits.OSPlatform.Linux, true) == 0)
            {
                platform = AMToolkits.OSPlatform.Linux;
            }
            else if (string.Compare(value, AMToolkits.OSPlatform.MacOS, true) == 0)
            {
                platform = AMToolkits.OSPlatform.MacOS;
            }
            else if (string.Compare(value, AMToolkits.OSPlatform.iOS, true) == 0)
            {
                platform = AMToolkits.OSPlatform.iOS;
            }
            else if (string.Compare(value, AMToolkits.OSPlatform.Android, true) == 0)
            {
                platform = AMToolkits.OSPlatform.Android;
            }
            else if (string.Compare(value, AMToolkits.OSPlatform.Web, true) == 0)
            {
                platform = AMToolkits.OSPlatform.Web;
            }

            os_desc = platform + "-" + os_desc;
            return platform;
        }


        public static void QueryString(this HttpContext context, string key, out string value, string defval = "")
        {
            value = defval;

            string[] values;
            QueryString(context, key, out values);
            if (values.Length > 0)
            {
                value = values[0];
            }
        }

        public static void QueryString(this HttpContext context, string key, out string[] values)
        {
            values = new string[] { };

            Microsoft.Extensions.Primitives.StringValues vs;
            // 获取timestamp参数并转换为整数
            if (!context.Request.Query.TryGetValue(key, out vs))
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
        /// NOTE: Not use method :context.Response.WriteAsJsonAsync
        /// and not set ContentLength
        /// </summary>
        /// <param name="context"></param>
        /// <param name="content"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private static async Task ResponseJsonAsync(this HttpContext context, object content, HttpStatusCode code = HttpStatusCode.OK)
        {
            context.Response.StatusCode = (int)code;
            context.Response.ContentType = "application/json";

            // await context.Response.WriteAsJsonAsync(content,
            //     new System.Text.Json.JsonSerializerOptions
            //     {
            //         IgnoreReadOnlyFields = true,
            //         IncludeFields = true,
            //         // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
            //         ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
            //         // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
            //         PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            //     });

            string json = System.Text.Json.JsonSerializer.Serialize(content,
                new System.Text.Json.JsonSerializerOptions
                {
                    IgnoreReadOnlyFields = true,
                    IncludeFields = true,
                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });
            //
            var encoding = System.Text.Encoding.UTF8;
            long length = System.Text.Encoding.UTF8.GetByteCount(json);
            //
            context.Response.ContentLength = length;
            // Not use method :context.Response.WriteAsJsonAsync
            // and not set ContentLength
            await context.Response.WriteAsync(json, encoding);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public async static Task ResponseStatusAsync(this HttpContext context, string status,
                                    string message = "",
                                    HttpStatusCode code = HttpStatusCode.OK)
        {
            await context.ResponseStatusAsync(status, message, 0, 0.0f, code);
        }


        public async static Task ResponseStatusAsync(this HttpContext context, string status,
                                    string message,
                                    long timestamp = 0, float delay = 0.0f,
                                    HttpStatusCode code = HttpStatusCode.OK)
        {
            if (timestamp <= 0)
            {
                timestamp = AMToolkits.Utils.GetLongTimestamp();
            }
            if (delay < AMToolkits.NetworkStatus.NETWORK_DELAY_MIN)
            {
                delay = AMToolkits.NetworkStatus.NETWORK_DELAY_MIN;
            }

            var address = context.GetClientAddress();

            // 统一规范
            var result = new
            {
                Code = code,
                Status = status,
                Message = message,
                Timestamp = timestamp,
                Delay = delay,
                DateTime = DateTime.UtcNow,
                Address = address.ToString()
            };
            await context.ResponseJsonAsync(result, code);
        }

        public async static Task ResponseError(this HttpContext context, HttpStatusCode code = HttpStatusCode.BadRequest, string message = "")
        {
            object? data = null;
            if (message.Length == 0)
            {
                message = "Bad Request";
            }

            var result = new
            {
                Code = code,
                Status = "error",
                Message = message,
                Data = data
            };
            await context.ResponseJsonAsync(result, code);
        }


        public async static Task ResponseResult(this HttpContext context, object? data = null, HttpStatusCode code = HttpStatusCode.OK)
        {
            var result = new
            {
                Code = code,
                Status = "success",
                Data = data
            };
            await context.ResponseJsonAsync(result, code);
        }
        
    }
}