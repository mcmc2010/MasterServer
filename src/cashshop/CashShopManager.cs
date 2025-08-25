using Logger;
using Microsoft.AspNetCore.Builder;

using AMToolkits.Extensions;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserCashShopItem
    {
        public int uid = 0;
        public string id = ""; //流水单号
        public string user_id = ""; //t_user表中的
        public string custom_id = "";
        public string user_name = "";
        public string product_id = "";
        public string name = "";
        public int type = 0;
        public int count = 0;
        public int amount = 0;
        public int balance = 0;
        public string? item_0 = "";
        public string? item_1 = "";
        public string? item_2 = "";

        public DateTime? create_time = null;
        public DateTime? last_time = null;

        public int status = 0;

        private AMToolkits.Utility.ITableData? _template_data = null;

        public void InitTemplateData<T>(T templete_data) where T : AMToolkits.Utility.ITableData
        {
            _template_data = templete_data;
        }

        public T? GetTemplateData<T>() where T : AMToolkits.Utility.ITableData
        {
            return (T?)this._template_data;
        }

    }


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
        /// 统计列表花费
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public double TotalCashItemsCost(List<UserCashShopItem> items)
        {
            double value = 0;
            foreach (var v in items)
            {
                value += v.amount;
            }
            return System.Math.Abs(value);
        }

        /// <summary>
        /// 购买产品
        /// </summary>
        /// <param name="user_uid">玩家ID</param>
        /// <param name="id">流水单号</param>
        /// <param name="index">索引</param>
        public async System.Threading.Tasks.Task<Server.BuyProductResult> BuyProduct(string user_uid, string id, string item_id)
        {
            Server.BuyProductResult result = new BuyProductResult()
            {
                Code = -1,
                ID = id,
            };

            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return result;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == item_id);
            if (shop_template_item == null || shop_template_item.ShopType != (int)AMToolkits.Game.ShopType.CashShop)
            {
                return result;
            }

            // 获取花费
            // 没有花费，物品不能购买
            var costs = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_template_item.Cost);
            if (costs.IsNullOrEmpty())
            {
                result.Code = 0;
                return result;
            }

            // 目前只支持单一扣除
            var cost = AMToolkits.Game.ItemUtils.GetVirtualCurrency(costs, AMToolkits.Game.ItemConstants.ID_NONE);
            // 必须有消耗, 目前商城没有金币购买物品
            if (cost == null || cost.ID != AMToolkits.Game.ItemConstants.ID_GM)
            {
                result.Code = 0;
                return result;
            }

            // 当原价大于等于10折扣才会生效
            float amount = -cost.Count;
            if (shop_template_item.Discount > 0)
            {
                amount = -(int)System.Math.Round(AMToolkits.Game.ItemUtils.GetDiscountPrice(cost.Count, shop_template_item.Discount));
            }

            // 获取道具
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_template_item.Items);
            if (items.IsNullOrEmpty())
            {
                result.Code = 0;
                return result;
            }
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                result.Code = 0;
                return result;
            }

            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null)
            {
                result.Code = -2; //未验证
                return result;
            }

            // 需要对齐
            int index = 1000;
            foreach (var v in item_list)
            {
                v.NID = ++index;
            }

            var r_result = await PlayFabService.Instance.PFCashShopBuyProduct(r_user.ID, r_user.CustomID,
                                id, shop_template_item.ProductId, item_list, amount, AMToolkits.Game.VirtualCurrency.GM, "cashshop");
            if (r_result == null)
            {
                result.Code = -1;
                _logger?.LogError($"{TAGName} (BuyProduct) (User:{user_uid}) {item_id} - {shop_template_item.Name} Amount: {amount} {AMToolkits.Game.VirtualCurrency.GM} Failed");
                return result;
            }

            // 添加数据库记录
            if (await UserManager.Instance._CashshopBuyProduct(r_user.ID, r_user.CustomID, r_result.Data) <= 0)
            {
                result.Code = 0;
                _logger?.LogWarning($"{TAGName} (BuyProduct) (User:{user_uid}) {item_id} - {shop_template_item.Name} Amount: {amount} {AMToolkits.Game.VirtualCurrency.GM} Failed");
                //return result;
            }


            //
            _logger?.Log($"{TAGName} (BuyProduct) Balance {r_result.Data?.Balance} ({r_result.Data?.VirtualCurrency}), Amount {amount} ({shop_template_item.Discount})");
            result.Code = 1;
            return result;
        }


        #region Server Internal
        /// <summary>
        /// 如果炸了，说明玩家消费超过1000条
        /// 哈哈哈哈
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _GetUserCashItems(string user_uid,
                            List<UserCashShopItem> items)
        {
            items.Clear();


            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }

            List<UserCashShopItem> list = new List<UserCashShopItem>();
            if (await this.DBGetUserCashItems(user_uid, list) < 0)
            {
                return -1;
            }

            foreach (var item in list)
            {
                // 物品必须是商城物品
                var template_item = shop_template_data.First(v => v.ProductId == item.product_id);
                if (template_item == null || template_item.ShopType != (int)AMToolkits.Game.ShopType.CashShop)
                {
                    continue;
                }

                item.InitTemplateData<Game.TShop>(template_item);

                items.Add(item);
            }

            return list.Count;
        }

        #endregion

    }
}