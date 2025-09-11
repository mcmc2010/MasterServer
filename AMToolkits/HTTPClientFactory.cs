
using System.Net;
using System.Threading.Tasks;

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
        private HTTPClientProxy? _client = null;

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
        }

        public HTTPClientProxy? APICreate(string url, float timeout = 5.0f)
        {
            url = url.Trim();
            if (url.Length == 0)
            {
                return null;
            }

            _url = url;
            if (_client == null || _client.IsRunning)
            {
                //
                _client = this.Create(url, timeout);
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

            return await _client.GetAsync<T>(endpoint, arguments, headers);
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

            return await _client.PostAsync<T>(endpoint, payload, headers, arguments);
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