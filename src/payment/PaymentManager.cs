
using Microsoft.AspNetCore.Builder;
using Logger;

namespace Server
{
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
        public string virtual_currency = "GEM";

        //
        public string channel = "";
        public string? payment_method = null;

        public DateTime? create_time = null;
        public DateTime? update_time = null;
        public DateTime? complete_time = null;

        public string? custom_data = "";
        public string? result_code = "";

        public int status = 0;
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager : AMToolkits.SingletonT<PaymentManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static PaymentManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

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


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.MapPost("api/payment/v1/transaction", HandleTransactionV1);

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

            var r_result = await DBCreateTransaction(r_user.ID, transaction);
            if (r_result <= 0)
            {
                return -1;
            }

            return 1;
        }
    }
}