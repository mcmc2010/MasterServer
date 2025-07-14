using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;
using MySqlX.XDevAPI.Common;

namespace Server
{
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
        public async System.Threading.Tasks.Task<int> BuyProduct(string user_uid, string id, int index)
        {
            if (index <= 0)
            {
                return 0;
            }

            //
            var shop_item = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>()?.Get(index);
            if (shop_item == null || shop_item.Id != index)
            {
                return -1;
            }
            else if (shop_item.ShopType != (int)AMToolkits.Game.ShopType.Market)
            {
                return -2; //错误的产品
            }

            // 获取花费
            var costs = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_item.Cost);
            if (costs.IsNullOrEmpty())
            {
                return 0;
            }

            // 目前只支持单一扣除
            var cost = AMToolkits.Game.ItemUtils.GetVirtualCurrency(costs, AMToolkits.Game.ItemConstants.ID_NONE);
            // 必须有消耗
            if (cost == null)
            {
                return 0;
            }

            // 获取道具
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_item.Items);
            if (items.IsNullOrEmpty())
            {
                return 0;
            }

            var item_gd = AMToolkits.Game.ItemUtils.GetVirtualCurrency(items, AMToolkits.Game.ItemConstants.ID_GD);
            var item_gm = AMToolkits.Game.ItemUtils.GetVirtualCurrency(items, AMToolkits.Game.ItemConstants.ID_GM);
            var item_list = AMToolkits.Game.ItemUtils.GetGeneralItems(items);

            // 首先 : 扣除货币
            Dictionary<string, object?>? result = null;
            if (cost.ID == AMToolkits.Game.ItemConstants.ID_GM)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, -cost.Count, AMToolkits.Game.VirtualCurrency.GM);
            }
            else if (cost.ID == AMToolkits.Game.ItemConstants.ID_GD)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, -cost.Count, AMToolkits.Game.VirtualCurrency.GD);
            }
            else
            {
                return 0;
            }

            // 扣除货币失败
            if (result == null)
            {
                return -1;
            }

            // 默认为int32，此处用浮点表示
            float balance = System.Convert.ToSingle(result.Get("balance") ?? 0.0f);
            string currency = System.Convert.ToString(result.Get("currency")) ?? AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;

            // 发放物品 :
            if (item_list != null)
            {
                result = await PlayFabService.Instance.PFAddInventoryItems(user_uid, item_list);
            }

            // 最后 : 给予货币
            if (item_gd != null)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, item_gd.Count, AMToolkits.Game.VirtualCurrency.GD);
            }
            if (item_gm != null) {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, item_gm.Count, AMToolkits.Game.VirtualCurrency.GM);
            }
            // 货币结算失败
            if (result == null)
            {
                return 0;
            }
            return 1;
        }
    }
}