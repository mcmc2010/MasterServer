using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;

namespace Server
{
    public enum ShopType
    {
        Market = 10,
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class MarketManager : AMToolkits.SingletonT<MarketManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static MarketManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public MarketManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[MarketManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.MapPost("api/market/buy", HandleMarketBuyProduct);

        }

        /// <summary>
        /// 从市场购买产品
        /// </summary>
        /// <param name="user_uid">玩家ID</param>
        /// <param name="id">流水单号</param>
        /// <param name="index">索引</param>
        public int BuyProduct(string user_uid, string id, int index)
        {
            if (index <= 0)
            {
                return 0;
            }

            var shop_item = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>()?.Get(index);
            if (shop_item == null || shop_item.Id != index)
            {
                return -1;
            }
            else if (shop_item.ShopType != (int)ShopType.Market)
            {
                return -2; //错误的产品
            }

            var items = AMToolkits.Game.ItemUtils.ParseGeneralItemData(shop_item.Items);
            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            return 1;
        }
    }
}