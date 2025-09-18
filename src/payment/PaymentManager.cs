
using Microsoft.AspNetCore.Builder;
using Logger;
using AMToolkits.Extensions;
using AMToolkits.Net;


using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public enum PaymentMethod
    {
        None = 0,
        Alipay = 200,
    }

    [System.Serializable]
    public class PaymentSettingItem_Ailpay
    {
        [JsonPropertyName("enabled")]
        public bool Enabled = false;

        [JsonPropertyName("is_sandbox")]
        public bool IsSandbox = false;

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("sandbox_url")]
        public string SandBoxURL { get; set; } = "";
        [JsonPropertyName("url")]
        public string URL { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("sandbox_app_id")]
        public string SandBoxAppID { get; set; } = "";
        [JsonPropertyName("app_id")]
        public string AppID { get; set; } = "";
        
        [JsonPropertyName("method")]
        public string Method { get; set; } = "";
    }

    [System.Serializable]
    public class PaymentSettings
    {
        [JsonPropertyName("ssl_certs")]
        public string SSLCertificates { get; set; } = "";
        [JsonPropertyName("ssl_key")]
        public string SSLKey { get; set; } = "";

        /// <summary>
        /// 订单超时设置
        /// </summary>
        [JsonPropertyName("transaction_timeout")]
        public float TransactionTimeout = 30.0f;


        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("alipay")]
        public PaymentSettingItem_Ailpay Alipay = new PaymentSettingItem_Ailpay();
    }


    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class TransactionItem
    {
        public int uid = 0;
        public string id = ""; //流水单号
        public string order_id = ""; //订单号
        public string user_id = ""; //t_user表中的
        public string custom_id = "";
        public string product_id = "";
        public string name = "";
        public int type = 1;
        public int sub_type = 0;
        public int count = 0;
        public double amount = 0.00f;
        public double price = 0.00f;
        public double fee = 0.00f; // 手续费

        public string currency = "CNY";
        public double virtual_amount = 0.00f;
        public string virtual_currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;

        //
        public string channel = "";
        public string? payment_method = null;

        public DateTime? create_time = null;
        public DateTime? update_time = null;
        public DateTime? complete_time = null;

        public string? custom_data = "";
        public string? result_code = "";

        public int status = 0;

        // 这个是审核时间，默认不需要序列化
        public DateTime? ReviewTime = null;
        public int ReviewCount = 0;

        /// <summary>
        /// 将item同步到类对象
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Clone(TransactionItem item)
        {
            if ((!this.id.IsNullOrWhiteSpace() && item.id != this.id) ||
                (!this.order_id.IsNullOrWhiteSpace() && item.order_id != this.order_id))
            {
                return false;
            }

            // Copy all fields from source item
            this.uid = item.uid;
            this.id = item.id;
            this.order_id = item.order_id;
            this.user_id = item.user_id;
            this.custom_id = item.custom_id;
            this.product_id = item.product_id;
            this.name = item.name;
            this.type = item.type;
            this.sub_type = item.sub_type;
            this.count = item.count;
            this.amount = item.amount;
            this.price = item.price;
            this.fee = item.fee;
            this.currency = item.currency;
            this.virtual_amount = item.virtual_amount;
            this.virtual_currency = item.virtual_currency;
            this.channel = item.channel;
            this.payment_method = item.payment_method;
            this.create_time = item.create_time;
            this.update_time = item.update_time;
            this.complete_time = item.complete_time;
            this.custom_data = item.custom_data;
            this.result_code = item.result_code;
            this.status = item.status;
            return true;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager : AMToolkits.SingletonT<PaymentManager>, AMToolkits.ISingleton
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly string[] REASONS = new string[]
        {
            "none",
            "timeout",
            "error",
            "completed",
            "pending",
            "notfound", // 订单不存在
            "review",   //自动审核
            "approved", //客服审核通过
            "rejected", //客服审核拒绝
        };

        [AMToolkits.AutoInitInstance]
        protected static PaymentManager? _instance;

        /// <summary>
        /// 
        /// </summary>
        private static PaymentSettings _settings = new PaymentSettings()
        {

        };

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private System.Security.Cryptography.X509Certificates.X509Certificate2? _certificate = null;

        private HTTPClientFactory? _client_factory = null;

        //
        private object _transactions_queue_locked = new object();

        private List<TransactionItem> _transactions_queue = new List<TransactionItem>();

        public PaymentManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[PaymentManager] Config is NULL.");
                return;
            }
            _config = config;


            //
            this.InitLogger("payment");

            this.LoadSettings("payment_settings.txt");

            //
            this.LoadCertificate(_settings.SSLCertificates, _settings.SSLKey);

            //
            _client_factory = HTTPClientFactory.CreateFactory<HTTPClientFactory>();
            if (_settings.Alipay.Enabled)
            {
                string base_url = _settings.Alipay.URL;
                if (_settings.Alipay.IsSandbox)
                {
                    base_url = _settings.Alipay.SandBoxURL;
                }
                _client_factory.APICreate(base_url, 1.0f);
                _client_factory.OnLogOutput = (client, message) =>
                {
                    _logger?.Log($"{TAGName} (OpenAPI) [{client?.Index}]: {message}");
                };
            }

            //
        }

        private void InitLogger(string name)
        {
            //
            _logger = Logger.LoggerFactory.Instance;
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

        private void LoadSettings(string filename)
        {
            try
            {
                var asset = AMToolkits.Utility.ResourcesManager.Load<AMToolkits.Utility.TextAsset>(filename);
                if (asset == null)
                {
                    return;
                }

                var o = System.Text.Json.JsonSerializer.Deserialize<PaymentSettings>(asset.text,
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
                if (o != null)
                {
                    _settings = o;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TAGName} Loading Settings : {ex.Message}");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private System.Security.Cryptography.X509Certificates.X509Certificate2? LoadCertificate(string cert_filename, string key_filename)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(cert_filename) ?? "cert";
            System.Security.Cryptography.X509Certificates.X509Certificate2? cert;
            if (_certificate != null)
            {
                _certificate.Dispose();
                _certificate = null;
            }

            // 读取证书
            string cert_pem = "";
            if (cert_filename.Trim().Length > 0)
            {
                cert_pem = File.ReadAllText(cert_filename);
            }

            string key_pem = "";
            if (key_filename.Trim().Length > 0)
            {
                key_pem = File.ReadAllText(key_filename);
            }

            if (string.IsNullOrEmpty(cert_pem) || string.IsNullOrEmpty(key_pem))
            {
                return null;
            }

            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(key_pem);

            // windows 该方法无法导出私钥
            cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(cert_pem, key_pem);
            // 导出为PFX格式
            var p12 = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, (string?)null);
            // 导出可用私钥
            cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(p12, (string?)null,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.UserKeySet);

            _certificate = cert;

            _logger?.Log($"{TAGName} Load Certificate ({name}) : {cert.GetSerialNumberString()} ");
            return cert;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.MapPost("api/payment/v1/transaction/start", HandleTransactionV1);
            args.app?.MapPost("api/payment/v1/transaction/final", HandleTransactionFinalV1);
            args.app?.MapPost("api/payment/v1/transaction/check", HandleCheckTransactionV1);
        }


#pragma warning disable CS4014
        public int StartWorking()
        {
            _transactions_queue.Clear();

            //
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
                int count = 0;
                lock (_transactions_queue_locked)
                {
                    count = _transactions_queue.Count;
                }

                if (count > 0)
                {
                    TransactionItem? transaction = null;
                    lock (_transactions_queue_locked)
                    {
                        transaction = _transactions_queue.ElementAt(0);
                    }
                    
                    if (await _UpdateTransactionItem(transaction) >= 0)
                    {
                        lock (_transactions_queue_locked)
                        {
                            _transactions_queue.Remove(transaction);
                        }
                    }
                }
                await Task.Delay((int)(delay * 1000));
            }

            return 0;
        }

        private async Task<int> _UpdateTransactionItem(TransactionItem? transaction)
        {
            if (transaction == null)
            {
                return 0;
            }

            if (transaction.ReviewTime == null)
            {
                return 0;
            }

            // 小于5秒的订单，暂时不审核
            TimeSpan? timespan = (DateTime.UtcNow - transaction.ReviewTime);
            if (transaction.ReviewCount == 1 && timespan?.TotalSeconds < 5.0f)
            {
                return -2;
            }
            else if (transaction.ReviewCount == 2 && timespan?.TotalSeconds < 60.0f)
            {
                return -2;
            }
            else if (transaction.ReviewCount == 3 && timespan?.TotalSeconds < _settings.TransactionTimeout)
            {
                return -2;
            }

            transaction.ReviewCount++;
            var result = await AlipayGetTransactionData(transaction.user_id, transaction);
            if (result == null) {
                return -1;
            }

            if (result.Status == RESULT_REASON_TRADE_NOT_EXIST)
            {
                // 暂时不处理，等待下次审核
                if (transaction.ReviewCount < 4)
                {
                    return -3;
                }

                await DBReviewTransaction(transaction.user_id, transaction, "timeout");
                return 0;
            }
            else if (result.Status == RESULT_REASON_SUCCESS)
            {
                // 
                await ExtractTransaction_V1(transaction.user_id, transaction, "review");

                await DBReviewTransaction(transaction.user_id, transaction, "review");
                _logger?.Log($"{TAGName} (UpdateTransactionItem) [{_transactions_queue.Count}]: ({transaction.user_id}) {transaction.order_id} ({result.Status}) Final [Review]");


            }
            else
            {
                _logger?.LogError($"{TAGName} (UpdateTransactionItem) : ({transaction.user_id}) {transaction.order_id} ({result.Status})");
                return -1;
            }

            return 1;
        }
    }
}