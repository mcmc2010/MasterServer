////
/// Fixed : request_duration show error;

using System.Linq;
using System.Net;
using AMToolkits.Extensions;

////
namespace AMToolkits.Net
{
    /// <summary>
    /// 
    /// </summary>
    public enum HTTPMethod
    {
        Get = 0,
        Post = 1
    }

    public interface IHTTPResponseResult
    {
    }

    public class HTTPError
    {
        public int Code;
        public string Message;

        public HTTPError(int code, string message)
        {
            this.Code = code;
            this.Message = message;
        }
    }

    [System.Serializable]
    public class HTTPResponseResult : IHTTPResponseResult
    {
        public int Code;
        public string Status = "";
        public string Message = "";
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public long Timestamp = 0;
        [System.Text.Json.Serialization.JsonPropertyName("date_time")]
        public string DateTime = "";
    }

    [System.Serializable]
    public class HTTPResponseData : HTTPResponseResult
    {
        public object? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public class HTTPClientProxy : IFactoryObject
    {
        private long _index = -1;
        public long Index => _index;
        public void _SetIndex(long index) { _index = index; }

        private string _base_url = "";
        private string _url = "";
        public string Url { get { return _url; } }
        public string GetEndPoint()
        {
            if (_url.IsNullOrWhiteSpace())
            {
                return "";
            }

            // 匹配路径部分（排除协议、域名、查询参数、锚点）
            var pattern = @"(?:https?://[^/]+)?(/[^?#]*)";
            var match = System.Text.RegularExpressions.Regex.Match(_url, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return "/";
        }
        private string _full_url = "";

        private int _timeout_limit = 5 * 1000;
        /// <summary>
        /// 单位秒
        /// </summary>
        private float _request_duration = 0.0f; // 单位毫秒
        public float DurationTimeMS { get { return _request_duration; } }
        public float DurationTime { get { return _request_duration * 0.001f; } }

        private HttpStatusCode _status_code = HttpStatusCode.OK;
        public int StatusCode { get { return (int)_status_code; } }

        private string? _status_error = null;
        public HTTPError? LastError
        {
            get
            {
                return _status_code == HttpStatusCode.OK ? null :
                    new HTTPError((int)_status_code, _status_error ?? _data);
            }
        }
        private string _data = "";
        private IHTTPResponseResult? _body = null;

        /// <summary>
        /// 
        /// </summary>
        private FactoryObjectStatus _status = FactoryObjectStatus.None;
        public bool IsRunning { get { return _status == FactoryObjectStatus.Running; } }

        private bool _has_compress = false;
        private bool _is_output_log = true;
        public System.Action<object?, string>? OnLogOutput = null;

        private HttpClient? _http_client;
        private System.Threading.CancellationTokenSource? _cts;
        private readonly object _locked = new object();

        public T? BodyT<T>() where T : IHTTPResponseResult
        {
            return (T?)_body;
        }

        /// <summary>
        /// Factory
        /// </summary>
        public HTTPClientProxy()
        {
        }

        public HTTPClientProxy(string base_url,
                    float timeout = 5.0f,
                    bool has_compress = false)
        {
            this.Initialize(base_url, timeout, has_compress);
        }

        public virtual void Initialize(params object[] args)
        {
            _base_url = ((string?)args.ElementAtOrDefault(0) ?? "").Trim().ToLower().TrimEnd('/');
            if (!_base_url.StartsWith("http"))
            {
                _base_url = "";
            }
            // default: false
            _has_compress = (bool?)args.ElementAtOrDefault(2) ?? false;
            // ms
            _timeout_limit = (int)(((float?)args.ElementAtOrDefault(1) ?? 5.0f) * 1000.0f);

            //
            _http_client = new HttpClient(new System.Net.Http.HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = this.HandleServerCertificateValidation
            });

            //
            _status = FactoryObjectStatus.Start;
        }

        public void Dispose()
        {
            this.OnLogOutput = null;

            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            _cts?.Dispose();

            if (_http_client != null)
            {
                _http_client.Dispose();
                _http_client = null;
            }

            _status = FactoryObjectStatus.Release;
        }

        private bool HandleServerCertificateValidation(System.Net.Http.HttpRequestMessage request,
            System.Security.Cryptography.X509Certificates.X509Certificate2? certificate,
            System.Security.Cryptography.X509Certificates.X509Chain? chain,
            System.Net.Security.SslPolicyErrors errors)
        {
            if (errors != System.Net.Security.SslPolicyErrors.None &&
                errors != (System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch
                | System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors))
            {
                return false;
            }
            return true;
        }

        void Log(params object[] args)
        {
            string log = string.Join(" ", args.Select(v =>
            {
                if (v is string) { return (string)v; }
                else { return v.ToString(); }
            }));

            //
            this.OnLogOutput?.Invoke(this, log);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<T?> GetAsync<T>(string url,
            Dictionary<string, object>? arguments = null,
            Dictionary<string, object>? headers = null,
            System.Action<HTTPClientProxy, object?>? callback = null)
            where T : IHTTPResponseResult
        {
            return await HTTPClientProxy.ProcessRequestAsync<T>(this, HTTPMethod.Get, url, null, headers, arguments, callback);
        }

        public async Task<T?> PostAsync<T>(string url, object? payload,
                Dictionary<string, object>? headers = null,
                Dictionary<string, object>? arguments = null,
                System.Action<HTTPClientProxy, object?>? callback = null)
                where T : IHTTPResponseResult
        {
            return await HTTPClientProxy.ProcessRequestAsync<T>(this, HTTPMethod.Post, url, payload, headers, arguments, callback);
        }


        /// <summary>
        /// 精度秒
        /// </summary>
        /// <returns></returns>
        public static UInt32 Timestamp
        {
            get
            {
                DateTime tn = DateTime.UtcNow;
                DateTime t0 = new DateTime(1970, 1, 1);
                TimeSpan span = tn - t0;
                return (UInt32)span.TotalSeconds;
            }
        }
        /// <summary>
        /// 精度毫秒
        /// </summary>
        public static long LongTimestamp
        {
            get
            {
                DateTime tn = DateTime.UtcNow;
                DateTime t0 = new DateTime(1970, 1, 1);
                TimeSpan span = tn - t0;
                return (long)span.TotalMilliseconds;
            }
        }

        /// <summary>
        /// 精度微秒
        /// </summary>
        public static double PrecisionTimestamp
        {
            get
            {
                DateTime tn = DateTime.UtcNow;
                DateTime t0 = new DateTime(1970, 1, 1);
                TimeSpan span = tn - t0;
                return span.TotalMicroseconds;
            }
        }

        private static void AppendHeader(HttpRequestMessage request, string key, string? value)
        {
            if (request.Headers.Contains(key))
            {
                request.Headers.Remove(key);
            }
            request.Headers.Add(key, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ParseArguments(string url, Dictionary<string, object>? args = null)
        {
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }

            string text = "";
            bool first = url.Contains("/?") == false;
            if (args != null)
            {
                foreach (var pair in args)
                {
                    if (first)
                    {
                        text += url.IsNullOrWhiteSpace() ? "" : "?";
                        first = false;
                    }
                    else
                    {
                        text += "&";
                    }
                    text += $"{pair.Key}={pair.Value}";
                }
            }

            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex JsonPattern = new System.Text.RegularExpressions.Regex(
            @"^\s*(\{[\s\S]*\}|\[[\s\S]*\])\s*$",
            System.Text.RegularExpressions.RegexOptions.Compiled
        );

        private static bool IsJsonBody(string content)
        {
            if (!JsonPattern.IsMatch(content))
            {
                return false;
            }
            return true;
        }

        private static T? ParseJsonBody<T>(string content) where T : IHTTPResponseResult
        {
            try
            {
                if (!JsonPattern.IsMatch(content))
                {
                    return default(T);
                }

                var body = System.Text.Json.JsonSerializer.Deserialize<T>(content,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                                                                                      // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                });
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
        /// <param name="data"></param>
        /// <returns></returns>
        private static string ParsePayload(object? data)
        {
            if (data == null)
            {
                return "";
            }

            try
            {
                if (data is string s)
                {
                    return (string)s;
                }
                else if (data is IDictionary<string, object> pairs)
                {
                    return System.Text.Json.JsonSerializer.Serialize(pairs,
                                    new System.Text.Json.JsonSerializerOptions
                                    {
                                        IgnoreReadOnlyFields = true,
                                        IncludeFields = true,
                                        // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                                                                                          // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                    });
                }
                else if (data is System.Array ar)
                {
                    return System.Text.Json.JsonSerializer.Serialize(ar,
                                    new System.Text.Json.JsonSerializerOptions
                                    {
                                        IgnoreReadOnlyFields = true,
                                        IncludeFields = true,
                                        // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                                                                                          // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                    });
                }
                else
                {
                    return System.Convert.ToString(data) ?? "";
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected static async Task<T?> ProcessRequestAsync<T>(HTTPClientProxy client, HTTPMethod method, string url, object? payload = null,
                                    Dictionary<string, object>? headers = null,
                                    Dictionary<string, object>? arguments = null,
                                    System.Action<HTTPClientProxy, object?>? callback = null)
                                    where T : IHTTPResponseResult
        {
            url = url.Trim();

            FactoryObjectStatus status = FactoryObjectStatus.None;
            lock (client._locked)
            {
                status = client._status;
            }

            //
            if (client._http_client == null || status == FactoryObjectStatus.Running)
            {
                callback?.Invoke(client, null);
                return default(T);
            }

            lock (client._locked)
            {
                client._status = FactoryObjectStatus.Running;
                client._cts = new CancellationTokenSource(System.TimeSpan.FromMilliseconds(client._timeout_limit));
            }
            status = FactoryObjectStatus.Running;

            // 修正url
            if (client._base_url.Length > 0)
            {
                if (!url.StartsWith("/") && url.Length > 0)
                {
                    url = $"{client._base_url}/{url}";
                }
                else
                {
                    url = $"{client._base_url}{url}";
                }
            }

            //
            client._url = url;
            client._request_duration = 0.0f;
            client._status_code = HttpStatusCode.OK;
            client._status_error = null;

            double timestamp = HTTPClientProxy.PrecisionTimestamp;

            //
            if (arguments != null)
            {
                url = url + HTTPClientProxy.ParseArguments(url, arguments);
            }

            client._full_url = url;
            if (client._is_output_log)
            {
                client.Log("[HTTP] (Request) Start:", client._url);
            }

            HttpRequestMessage request;
            if (method == HTTPMethod.Get)
            {
                request = new HttpRequestMessage(HttpMethod.Get, url);
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Post, url);
                //HTTPClientProxy.AppendHeader(request, "Content-Type", "application/json");

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(ParsePayload(payload));
                request.Content = new ByteArrayContent(buffer)
                {
                    Headers = {
                        ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                        {
                            CharSet = "utf-8"
                        }
                    }
                };
            }

            if (client._has_compress)
            {
                HTTPClientProxy.AppendHeader(request, "Accept-Encoding", "gzip");
            }
            // 附加自定义头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (!string.IsNullOrEmpty(header.Key))
                    {
                        string value = "";
                        if (header.Value is string)
                        {
                            value = (string)header.Value;
                        }
                        else
                        {
                            value = System.Convert.ToString(header.Value) ?? "";
                        }
                        HTTPClientProxy.AppendHeader(request, header.Key, value);
                    }
                }
            }

            try
            {

                var response = await client._http_client.SendAsync(request, client._cts.Token);
                client._status_code = response.StatusCode;
                client._request_duration = (float)(HTTPClientProxy.PrecisionTimestamp - timestamp) * 0.001f;

                //  无论成功与否都尝试解析body
                long content_length = 0;
                string content_type = "";
                if (response.Headers.TryGetValues("Content-Length", out var values) && !string.IsNullOrEmpty(values.FirstOrDefault()))
                {
                    content_length = System.Convert.ToInt64(values.FirstOrDefault());
                }
                else if (response.Content.Headers.TryGetValues("Content-Length", out values) || response.Content.Headers.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(values?.FirstOrDefault()))
                    {
                        content_length = System.Convert.ToInt64(values?.FirstOrDefault());
                    }
                    else
                    {
                        content_length = response.Content.Headers.ContentLength ?? 0;
                    }
                }
                if (response.Headers.TryGetValues("Content-Type", out values) && !string.IsNullOrEmpty(values.FirstOrDefault()))
                {
                    content_type = (values.FirstOrDefault() ?? "").Trim().ToLower();
                }
                else if (response.Content.Headers.TryGetValues("Content-Type", out values) && !string.IsNullOrEmpty(values.FirstOrDefault()))
                {
                    content_type = (values.FirstOrDefault() ?? "").Trim().ToLower();
                }

                T? body = default(T);
                string content = "";
                if (content_length > 0)
                {
                    var buffer = await response.Content.ReadAsByteArrayAsync();
                    content = System.Text.Encoding.UTF8.GetString(buffer);
                    if (content.Length > 0 &&
                        (content_type.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                        HTTPClientProxy.IsJsonBody(content)))
                    {
                        body = HTTPClientProxy.ParseJsonBody<T>(content);
                    }
                }
                client._data = content;
                client._body = body;

                //
                if (client._status_code != HttpStatusCode.OK)
                {
                    if (client._is_output_log)
                    {
                        client.Log("[HTTP] (Request) Failed:", $"({response.StatusCode})", client._url, $"[{client.DurationTimeMS:F2}ms]");
                        client.Log("[HTTP] (Response) Body:", client._data);
                    }
                    return default(T);
                }
                else
                {
                    if (client._is_output_log)
                    {
                        client.Log("[HTTP] (Request) Finish:", $"({response.StatusCode})", client._url, $"[{client.DurationTimeMS:F2}ms]");
                    }
                    return body;
                }

            }
            catch (TimeoutException e)
            {
                client._request_duration = (float)(HTTPClientProxy.PrecisionTimestamp - timestamp) * 0.001f;
                HandleTimeoutException(client, e);
                return default(T);
            }
            catch (Exception e)
            {
                client._request_duration = (float)(HTTPClientProxy.PrecisionTimestamp - timestamp) * 0.001f;

                
                //  按超时处理
                if (e.InnerException is TimeoutException e_timeout)
                {
                    HandleTimeoutException(client, e_timeout);
                }
                else if(e.InnerException is TaskCanceledException e_canceled)
                {
                    HandleUnknowException(client, e_canceled, true);
                }
                else
                {
                    HandleUnknowException(client, e, true);
                }
                return default(T);
            }
            finally
            {
                lock (client._locked)
                {
                    client._status = FactoryObjectStatus.Completed;
                }

                callback?.Invoke(client, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        protected static void HandleTimeoutException(HTTPClientProxy client, TimeoutException e)
        {
            //            
            client._status_code = HttpStatusCode.RequestTimeout;
            client._status_error = $"({e.HResult:X8}) {e.Message}";
            client.Log("[HTTP] (Request) Timeout:", e.Message, $"[{client.DurationTimeMS:F2}ms]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <param name="is_trace"></param>
        protected static void HandleUnknowException(HTTPClientProxy client, Exception e, bool is_trace = false)
        {
            //            
            client._status_code = HttpStatusCode.BadRequest;
            if(e is TaskCanceledException canceled)
            {
                client._status_code = HttpStatusCode.TooManyRequests;
            }
            client._status_error = $"({e.HResult:X8}) {e.Message}";
            client.Log("[HTTP] (Request) Error:", e.Message, $"[{client.DurationTimeMS:F2}ms]");
            if (is_trace)
            {
                client.Log("[HTTP] (Request) Trace: ");
                client.Log(e.StackTrace ?? "None");
            }
        }
    }
}
