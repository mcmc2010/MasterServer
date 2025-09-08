
using Microsoft.AspNetCore.Builder;
using Logger;
using AMToolkits.Extensions;
using System.Transactions;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Configuration;


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
        public bool enabled = false;

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
            "pending",
            "timeout",
            "error",
            "completed"
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

        /// <summary>
        /// 开始支付 - 创建订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> StartTransaction_V1(string user_uid, TransactionItem transaction)
        {
            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == transaction.product_id);
            if (shop_template_item == null || shop_template_item.ShopType != (int)AMToolkits.Game.ShopType.Shop_1)
            {
                return -1;
            }

            transaction.name = shop_template_item.Name;

            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return -2;
            }

            transaction.custom_id = r_user.CustomID;

            /// 支付方法：需要设置
            transaction.result_code = null;
            if (transaction.payment_method == "none" ||
               (!_settings.Alipay.enabled && transaction.payment_method?.Contains("alipay") == true))
            {
                transaction.result_code = "none";
            }

            var r_result = await DBCreateTransaction(r_user.ID, transaction);
            if (r_result <= 0)
            {
                return -1;
            }


            _logger?.Log($"{TAGName} (StartTransaction) : {transaction.id} - {transaction.name} " +
                    $"(User:{transaction.user_id}) {transaction.order_id} Amount: {transaction.amount} {transaction.currency} ");

            if (transaction.result_code == "none")
            {
                return -5;
            }
            return 1;
        }

        /// <summary>
        /// 开始支付 - 检测订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<string?> CheckTransaction_V1(string user_uid, TransactionItem transaction,
                            string? data)
        {
            if (data.IsNullOrWhiteSpace())
            {
                return null;
            }

            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return null;
            }

            transaction.custom_id = r_user.CustomID;

            // 对数据签名
            var buffer = data?.Base64UrlDecode();
            if (_certificate == null || buffer == null)
            {
                return "";
            }

            byte[]? sign_data = null;
            if (!AMToolkits.RSA.RSA2SignData(buffer, _certificate.GetRSAPrivateKey(), out sign_data) || sign_data == null)
            {
                return null;
            }

            var sign_b64 = sign_data.Base64UrlEncode();
            return sign_b64;
        }


        /// <summary>
        /// 开始支付 - 完成订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> FinalTransaction_V1(string user_uid, TransactionItem transaction,
                            string reason = "completed")
        {
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return -2;
            }

            // 原因必须在指定的选项中
            reason = reason.Trim().ToLower();
            if (!REASONS.Any(v => v == reason))
            {
                return -1;
            }

            transaction.custom_id = r_user.CustomID;

            /// 支付方法：需要设置
            if (transaction.payment_method == "none" ||
               (!_settings.Alipay.enabled && transaction.payment_method?.Contains("alipay") == true))
            {
                return -5;
            }

            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            var r_result = await DBGetTransactions(r_user.ID, transactions, transaction.id, transaction.order_id);
            if (r_result < 0)
            {
                return -1;
            }
            // 订单或流水号不存在
            var ti = transactions.FirstOrDefault();
            if (transactions.Count == 0 || ti == null)
            {
                return 0;
            }
            transaction.Clone(ti);

            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == transaction.product_id);
            if (shop_template_item == null || shop_template_item.ShopType != (int)AMToolkits.Game.ShopType.Shop_1)
            {
                return -1;
            }

            // 获取道具
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_template_item.Items);
            if (items.IsNullOrEmpty())
            {
                return -1;
            }
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                return -1;
            }

            // 只处理第一个配置物品
            var item = item_list.FirstOrDefault();
            if (item?.ID == AMToolkits.Game.ItemConstants.ID_GM)
            {
                transaction.virtual_currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
                transaction.virtual_amount = item.Count;
            }
            // 不支持 R 兑换游戏币（金币）
            else if (item?.ID == AMToolkits.Game.ItemConstants.ID_GD)
            {
                //transaction.virtual_currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
                //transaction.virtual_amount = item.Count;
            }
            else
            {
            }

            r_result = await DBFinalTransaction(r_user.ID, transaction, reason);
            if (r_result <= 0)
            {
                return -1;
            }

            //
            _logger?.Log($"{TAGName} (FinalTransaction) : {transaction.id} - {transaction.name} " +
                    $"(User:{transaction.user_id}) {transaction.order_id} Amount: {transaction.amount} {transaction.currency} ");
            return 1;
        }


        /// <summary>
        /// 修改订单 - 待审核
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _PendingTransaction(string user_uid, TransactionItem transaction)
        {
            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            var r_result = await DBGetTransactions(user_uid, transactions, transaction.id, transaction.order_id);
            if (r_result < 0)
            {
                return -1;
            }

            // 订单或流水号不存在
            var ti = transactions.FirstOrDefault();
            if (transactions.Count == 0 || ti == null)
            {
                return 0;
            }
            transaction.Clone(ti);

            r_result = await DBPendingTransaction(user_uid, transaction);
            if (r_result <= 0)
            {
                return -1;
            }

            return 1;
        }
    }
}