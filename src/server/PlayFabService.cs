
using System.Net;
using System.Text.Json.Serialization;

using AMToolkits.Extensions;
using AMToolkits.Net;
using Logger;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class PFResultBaseData
    {
        public string Result = "";
        public string? Error = null;
        public string? Description = null;
    }

    [System.Serializable]
    public class PFResultData : PFResultBaseData
    {
        public Dictionary<string, object?>? Data = null;
    }


    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class PFCheckServiceStatusResponse : AMToolkits.Net.HTTPResponseResult
    {
        public object? Data = null;
    }

    [System.Serializable]
    public class PFAuthUserRequest
    {
        [JsonPropertyName("uid")]
        public string UID = "";
        [JsonPropertyName("playfab_uid")]
        public string PlayFabUID = "";
        [JsonPropertyName("playfab_token")]
        public string PlayFabToken = "";
    }

    [System.Serializable]
    public class PFAuthUserResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFResultData? Data = null;
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class PlayFabService : AMToolkits.SingletonT<PlayFabService>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static PlayFabService? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;


        private HTTPClientFactory? _client_factory = null;
        private AMToolkits.ServiceStatus _status = AMToolkits.ServiceStatus.None;

        public PlayFabService()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[PlayFabService] Config is NULL.");
                return;
            }
            _config = config;

            this.InitLogger("playfab");


            //
            _client_factory = HTTPClientFactory.CreateFactory<HTTPClientFactory>();
            _client_factory.AddIgnoreEndPoint("/internal/services");
            if (!_config.PlayFab.OpenAPIUrl.IsNullOrWhiteSpace())
            {
                _client_factory.APICreate(_config.PlayFab.OpenAPIUrl, 5.0f);
                _client_factory.OnLogOutput = (client, message) =>
                {
                    _logger?.Log($"{TAGName} [{client?.Index ?? -1}:{_client_factory.PoolCount()}]: {message}");
                };
                _status = AMToolkits.ServiceStatus.Initialized;
            }
        }

        private void InitLogger(string name)
        {
            //
            _logger = null;
            var cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == name);
            if (cfg != null)
            {
                if (!cfg.Enabled)
                {
                    _logger = null;
                }
                else
                {
                    _logger = Logger.LoggerFactory.CreateLogger(cfg.Name, cfg.IsConsole, cfg.IsFile);
                    _logger.SetOutputFileName(cfg.File);
                }
            }
        }

#pragma warning disable CS4014
        public int StartWorking()
        {
            if (_status < AMToolkits.ServiceStatus.Initialized)
            {
                System.Console.WriteLine("[Server] PlayFab not initialize.");
                return -1;
            }       

            this.ProcessWorking();
            return 0;
        }
#pragma warning restore CS4014

        private async Task<int> ProcessWorking()
        {
            float delay = 5.0f;
            //
            while (!ServerApplication.Instance.HasQuiting)
            {
                if (_status == AMToolkits.ServiceStatus.Ready)
                {
                    await CheckServiceStatus();

                    delay = 60.0f;
                }
                else if (_status >= AMToolkits.ServiceStatus.Initialized)
                {
                    await CheckServiceStatus();

                    delay = 5.0f;
                }
                await Task.Delay((int)(delay * 1000));
            }
            return 0;
        }

        private async Task<bool> CheckServiceStatus()
        {
            if (_client_factory == null)
            {
                return false;
            }

            var response = await this.APIGet<PFCheckServiceStatusResponse>("/internal/services");
            if (response == null)
            {
                if (_client_factory.LastError?.Code == 400)
                {
                    System.Console.WriteLine($"{TAGName} Error: {_client_factory.LastError?.Message}");
                }
                else
                {
                    if (_client_factory.LastError?.Code == 401)
                    {
                        _status = AMToolkits.ServiceStatus.Refuse;
                    }
                    _logger?.LogError($"{TAGName} Error: ({_client_factory.LastError?.Code}) {_client_factory.LastError?.Message}");
                }
                return false;
            }
            else
            {
                if (_status == AMToolkits.ServiceStatus.Initialized)
                {
                    _status = AMToolkits.ServiceStatus.Ready;

                    await this.InitCompletad();
                }

                _status = AMToolkits.ServiceStatus.Ready;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected async Task InitCompletad()
        {
        }

        /// <summary>
        /// 用户验证
        /// </summary>
        /// <param name="client_uid"></param>
        /// <param name="playfab_uid"></param>
        /// <param name="playfab_token"></param>
        /// <returns></returns>
        public async Task<int> PFUserAuthentication(string client_uid, string playfab_uid, string playfab_token,
                                    string link_name, string link_id)
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return -1;
            }

            if (client_uid.IsNullOrWhiteSpace() || playfab_uid.IsNullOrWhiteSpace() || playfab_token.IsNullOrWhiteSpace())
            {
                return -1;
            }

            var response = await this.APICall<PFAuthUserResponse>("/internal/services/user/auth",
                    new Dictionary<string, object>()
                    {
                        { "client_uid", client_uid },
                        { "playfab_uid", playfab_uid },
                        { "playfab_token", playfab_token},
                        //
                        { "link_name", link_name },
                        { "link_id", link_id },

                    });
            if (response == null)
            {

                _logger?.LogError($"{TAGName} (User:{client_uid}) Authentication Failed: ({playfab_uid}) ({_client_factory?.LastStatusCode}) {_client_factory?.LastError?.Message}");

                if (_client_factory?.LastStatusCode == (int)HttpStatusCode.RequestTimeout ||
                   _client_factory?.LastStatusCode == (int)HttpStatusCode.TooManyRequests)
                {
                    return (int)AMToolkits.APIResultCode.TooMany;
                }
                return -1;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{client_uid}) Authentication Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return -1;
            }

            int result_code = 1;
            bool is_test_user = false;
            Dictionary<string, object?>? data = response.Data?.Data.ToDictionaryObject();
            if (data != null)
            {
                if (data.TryGetValue("test_user", out var value) && value is bool is_test && is_test == true)
                {
                    is_test_user = true;
                    result_code = 0; // Test User ot Guest User is Zero
                }
            }

            _logger?.Log($"{TAGName} (User:{client_uid}) Authentication : ({playfab_uid}) [{response.Data?.Result}]" +
                         $" {(!is_test_user ? "" : "[TestUser]")}" +
                         $" {(link_name.IsNullOrWhiteSpace() ? "" : "[" + link_name + "] " + link_id)}");

            return result_code;
        }




        
    }
}