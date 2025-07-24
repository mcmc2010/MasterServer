using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;


namespace Server
{
    namespace Market
    {
        [System.Serializable]
        public class BuyProductResult
        {
            public int Code = -1;
            public string ID = "";
            public List<AMToolkits.Game.GeneralItemData>? Items = null;
        }
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
        public async System.Threading.Tasks.Task<Server.Market.BuyProductResult> BuyProduct(string user_uid, string id, int index)
        {
            Server.Market.BuyProductResult b_result = new Market.BuyProductResult()
            {
                Code = -1,
                ID = id,
            };
            if (index <= 0)
            {
                b_result.Code = 0;
                return b_result;
            }

            //
            var shop_item = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>()?.Get(index);
            if (shop_item == null || shop_item.Id != index)
            {
                return b_result;
            }
            else if (shop_item.ShopType != (int)AMToolkits.Game.ShopType.Market)
            {
                return b_result; //错误的产品
            }

            // 获取花费
            var costs = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_item.Cost);
            if (costs.IsNullOrEmpty())
            {
                b_result.Code = 0;
                return b_result;
            }

            // 目前只支持单一扣除
            var cost = AMToolkits.Game.ItemUtils.GetVirtualCurrency(costs, AMToolkits.Game.ItemConstants.ID_NONE);
            // 必须有消耗
            if (cost == null)
            {
                b_result.Code = 0;
                return b_result;
            }

            // 当原价大于等于10折扣才会生效
            float amount = -cost.Count;
            if (shop_item.Discount > 0)
            {
                amount = (int)System.Math.Round(AMToolkits.Game.ItemUtils.GetDiscountPrice(cost.Count, shop_item.Discount));
            }

            // 获取道具
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_item.Items);
            if (items.IsNullOrEmpty())
            {
                b_result.Code = 0;
                return b_result;
            }

            var item_gd = AMToolkits.Game.ItemUtils.GetVirtualCurrency(items, AMToolkits.Game.ItemConstants.ID_GD);
            var item_gm = AMToolkits.Game.ItemUtils.GetVirtualCurrency(items, AMToolkits.Game.ItemConstants.ID_GM);
            var item_list = AMToolkits.Game.ItemUtils.GetGeneralItems(items);

            // 首先 : 扣除货币
            Dictionary<string, object?>? result = null;
            if (cost.ID == AMToolkits.Game.ItemConstants.ID_GM)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, amount, AMToolkits.Game.VirtualCurrency.GM);
            }
            else if (cost.ID == AMToolkits.Game.ItemConstants.ID_GD)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, amount, AMToolkits.Game.VirtualCurrency.GD);
            }
            else
            {
                b_result.Code = 0;
                return b_result;
            }

            // 扣除货币失败
            if (result == null)
            {
                return b_result;
            }

            // 默认为int32，此处用浮点表示
            float balance = System.Convert.ToSingle(result.Get("balance") ?? 0.0f);
            string currency = System.Convert.ToString(result.Get("currency")) ?? AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;

            // 发放物品 :
            if (item_list != null)
            {
                result = await PlayFabService.Instance.PFAddInventoryItems(user_uid, item_list);
                this.BuyProductFinalItems(user_uid, id, item_list.ToList(), result);
                b_result.Items = new List<AMToolkits.Game.GeneralItemData>(item_list);
            }

            // 最后 : 给予货币
            if (item_gd != null)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, item_gd.Count, AMToolkits.Game.VirtualCurrency.GD);
            }
            if (item_gm != null)
            {
                result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, item_gm.Count, AMToolkits.Game.VirtualCurrency.GM);
            }
            // 货币结算失败
            if (result == null)
            {
                b_result.Code = 0;
                return b_result;
            }

            //
            _logger?.Log($"{TAGName} (BuyProduct) Balance {balance} ({currency}), Amount {amount} ({shop_item.Discount})");

            b_result.Code = 1;
            return b_result;
        }


        private int BuyProductFinalItems(string user_uid, string id,
                                List<AMToolkits.Game.GeneralItemData> items,
                                Dictionary<string, object?>? result)
        {
            if (result == null) { return 0; }

            var o = result.Get("items", null);
            if (o == null) { return 0; }

            //
            int index = 0;
            if (o is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var v in elem.EnumerateArray())
                {
                    string item_id = "";
                    System.Text.Json.JsonElement iv;
                    if (v.TryGetProperty("item_id", out iv))
                    {
                        item_id = iv.GetString() ?? "";
                    }
                    if (v.TryGetProperty("iid", out iv))
                    {
                        string? s = iv.GetString();
                        if (!s.IsNullOrWhiteSpace())
                        {
                            if (items[index].ItemID == item_id)
                            {
                                items[index].IID = s ?? "";
                            }
                        }
                    }

                    index++;
                }
            }
            return index;
        }
    }
}