using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;


namespace Server
{

    [System.Serializable]
    public class BuyProductResult
    {
        public int Code = -1;
        public string ID = "";
        public List<AMToolkits.Game.GeneralItemData>? Items = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class CashShopManager : AMToolkits.SingletonT<CashShopManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static CashShopManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public CashShopManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[CashShopManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.MapPost("api/cashshop/buy", HandleCashShopBuyProduct);

        }

        /// <summary>
        /// 购买产品
        /// </summary>
        /// <param name="user_uid">玩家ID</param>
        /// <param name="id">流水单号</param>
        /// <param name="index">索引</param>
        public async System.Threading.Tasks.Task<Server.BuyProductResult> BuyProduct(string user_uid, string id, int index)
        {
            Server.BuyProductResult result = new BuyProductResult()
            {
                Code = -1,
                ID = id,
            };

            return result;
        }

    }
}