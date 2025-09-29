
using System.Net;
using System.Threading.Tasks;
using AMToolkits.Extensions;

namespace AMToolkits.Net
{

    [System.Serializable]
    public class APIResponseData<T> : HTTPResponseResult 
    {
        public object? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public class HTTPClientFactory : SchedulingFactory<HTTPClientProxy>
    {
        public static HTTPClientFactory Instance { get { return DefaultInstance<HTTPClientFactory>(); } }

        private string _url = "";
        private float _timeout = 5.0f;

        private float _duration_max = 0.0f;

        private List<string> _ignore_endpoints = new List<string>();
        public System.Action<HTTPClientProxy?, string>? OnLogOutput = null;

        /// <summary>
        ///  当前的实例
        /// </summary>
        private HTTPClientProxy? _client = null;
        private List<HTTPClientProxy> _client_queue = new List<HTTPClientProxy>();

        public HTTPError? LastError
        {
            get { return _client?.LastError; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HTTPClientFactory CreateDefaultFactory(string url)
        {
            if (HTTPClientFactory._instance == null)
            {
                var factory = CreateFactory<HTTPClientFactory>();
                factory.APICreate(url);
                HTTPClientFactory._instance = factory;
            }
            else
            {
                throw new Exception("The default implementation has already been created.");
            }
            return (HTTPClientFactory)HTTPClientFactory._instance;
        }

        public override void Initialize()
        {
            base.Initialize();

            _client_queue.Clear();

            _ignore_endpoints.Clear();
        }

        private void Log(HTTPClientProxy? client, params string[] args)
        {
            // 忽略某些endpoint
            string? endpoint = client?.GetEndPoint();
            if (!endpoint.IsNullOrWhiteSpace()
                && _ignore_endpoints.Any(v => string.Compare(v, endpoint, StringComparison.OrdinalIgnoreCase) == 0))
            {
                return;
            }

            string log = string.Join(" ", args.Select(v =>
            {
                if (v is string) { return (string)v; }
                else { return v.ToString(); }
            }));

            this.OnLogOutput?.Invoke(client, log);
        }

        public void AddIgnoreEndPoint(string endpoint)
        {
            endpoint = endpoint.Trim();
            if (endpoint.IsNullOrWhiteSpace())
            {
                return;
            }
            _ignore_endpoints.Add(endpoint);
        }

        public HTTPClientProxy? APICreate(string url, float timeout = 5.0f)
        {
            url = url.Trim();
            if (url.Length == 0)
            {
                return null;
            }

            _url = url;
            _timeout = timeout;

            //
            return _APICreate(_url, _timeout);
        }

        private HTTPClientProxy _APICreate(string url, float timeout = 5.0f)
        {

            if (_client == null || _client.IsRunning)
            {
                //
                _client = this.Create(url, timeout);
                _client.OnLogOutput = (sender, message) =>
                {
                    if (this.OnLogOutput == null)
                    {
                        System.Console.WriteLine(message);
                    }
                    else
                    {
                        this.Log((HTTPClientProxy?)sender, message);
                    }
                };
            }

            return _client;
        }

        public async Task<T?> GetAsync<T>(string endpoint,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null)
                        where T : HTTPResponseResult
        {
            if (_client == null)
            {
                return default(T);
            }
            if (_client.IsRunning)
            {
                var client_last = _client;
                _client = _APICreate(_url, _timeout);

                this.Log(_client, $"[HTTP] (Factory) : Create Instance ({_client.Index}, last: {client_last.Index})");
            }

            var result = await _client.GetAsync<T>(endpoint, arguments, headers,
                        (proxy, v) =>
                        {
                            if (proxy != _client)
                            {
                                this.Free(proxy);
                            }
                        });

            //
            this._duration_max = System.Math.Max(_duration_max, _client.DurationTime);
            return result;
        }

        public async Task<T?> PostAsync<T>(string endpoint, object? payload,
                        Dictionary<string, object>? headers = null,
                        Dictionary<string, object>? arguments = null)
                         where T : HTTPResponseResult
        {
            if (_client == null)
            {
                return default(T);
            }

            if (_client.IsRunning)
            {
                var client_last = _client;
                _client = _APICreate(_url, _timeout);

                this.Log(_client, $"[HTTP] (Factory) : Create Instance ({_client.Index}, last: {client_last.Index})");
            }

            var result = await _client.PostAsync<T>(endpoint, payload, headers, arguments,
                        (proxy, v) =>
                        {
                            if (proxy != _client)
                            {
                                this.Free(proxy);
                            }
                        });

            //
            this._duration_max = System.Math.Max(_duration_max, _client.DurationTime);
            return result;
        }

        public static async Task<T?> APIGetAsync<T>(string endpoint,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null)
                        where T : HTTPResponseResult
        {
            return await Instance.GetAsync<T>(endpoint, arguments, headers);
        }

        public static async Task<T?> APICallAsync<T>(string endpoint, object? payload,
                        Dictionary<string, object>? headers = null,
                        Dictionary<string, object>? arguments = null)
                        where T : HTTPResponseResult
        {
            return await Instance.PostAsync<T>(endpoint, payload, headers, arguments);
        }

    }
}