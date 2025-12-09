
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using AMToolkits.Game;

using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUserInventoryItemsResult
    {

        [JsonPropertyName("virtual_currency")]
        public Dictionary<int, object?>? VirtualCurrency = null;
        
        [JsonPropertyName("items")]
        public string ItemValues = "";
    }
    
    /// <summary>
    /// 
    /// </summary>
    public partial class InternalService
    {
        /// <summary>
        /// 获取钱包
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<WalletData?> _GetWalletData(string user_uid, string custom_uid)
        {
            user_uid = user_uid?.Trim() ?? "";
            custom_uid = custom_uid?.Trim() ?? "";
            if (user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            // 如果没有给出custom id将从在线用户中获取
            if (custom_uid.IsNullOrWhiteSpace())
            {
                var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
                if (user == null)
                {
                    return null;
                }

                //
                custom_uid = user.CustomID;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFGetWalletData(user_uid, custom_uid);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }


            AMToolkits.Game.WalletData wallet = new AMToolkits.Game.WalletData();

            // 
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS, out var gems))
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gems = System.Convert.ToSingle(gems);
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD, out var gold))
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gold = System.Convert.ToSingle(gold);

            //
            UserManager.Instance._UpdateWalletData(user_uid, wallet);
            return wallet;
        }


        ///
        /// 更新钱包虚拟货币
        /// 
        public async Task<Dictionary<string, object?>?> _UpdateVirtualCurrency(string user_uid, string custom_uid,
                            float amount = 0.0f,
                            AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GD,
                            string reason = "")
        {
            user_uid = user_uid?.Trim() ?? "";
            custom_uid = custom_uid?.Trim() ?? "";
            if (user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            // 如果没有给出custom id将从在线用户中获取
            if (custom_uid.IsNullOrWhiteSpace())
            {
                var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
                if (user == null)
                {
                    return null;
                }

                //
                custom_uid = user.CustomID;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, custom_uid, amount, currency, reason);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (UpdateVirtualCurrency) (User:{user_uid}) Amount: {amount} {currency} Failed");
                return null;
            }

            // 更新成功，更新本地数据库
            // 暂时不处理
            UserManager.Instance._UpdateWalletData(user_uid, result);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="custom_uid"></param>
        /// <param name="ItemValues"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<int> _AddUserInventoryItems(string user_uid, string custom_uid,
                            string item_values,
                            NUserInventoryItemsResult result,
                            string reason = "internal")
        {
            item_values = item_values.Trim();
            // 获取道具 (是否有物品发放)
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(item_values);
            if (items == null || items.Length == 0)
            {
                return 0;
            }

            //
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                _logger?.LogError($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) Add Items {item_values} Failed ({reason})");
                return -1;
            }

            // 1: 虚拟货币
            var list = item_list.Where(v => AMToolkits.Game.ItemUtils.HasVirtualCurrency(v.ID)).ToList();
            if (list.Count > 0)
            {
                item_list.RemoveAll(v => list.Contains(v));

                result.VirtualCurrency = new Dictionary<int, object?>();

                Dictionary<string, object?>? r_vc = null;
                foreach (var v in list)
                {
                    string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
                    if (v.ID == AMToolkits.Game.ItemConstants.ID_GD && v.Count > 0)
                    {
                        r_vc = await UserManager.Instance._UpdateVirtualCurrency(user_uid, v.Count, AMToolkits.Game.VirtualCurrency.GD, reason);

                        result.VirtualCurrency[0] = r_vc;
                    }
                    else if (v.ID == AMToolkits.Game.ItemConstants.ID_GM)
                    {
                        currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
                        r_vc = await UserManager.Instance._UpdateVirtualCurrency(user_uid, v.Count, AMToolkits.Game.VirtualCurrency.GM, reason);

                        result.VirtualCurrency[1] = r_vc;
                    }

                    if (r_vc == null)
                    {
                        _logger?.LogError($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) {v.ID} " +
                                          $"Amount: {v.Count} {currency} Failed");
                        break;
                    }
                }

                if (r_vc == null)
                {
                    return 0;
                }
            }


            // 2: 物品
            if (item_list.Count > 0)
            {
                // 发放物品 :
                var result_code = await UserManager.Instance._AddUserInventoryItems(user_uid, item_list, reason);
                if (result_code < 0)
                {
                    return -1;
                }

                result.ItemValues = AMToolkits.Game.ItemUtils.ToItemValue(item_list) ?? "";
            }
            return 1;
        }

        /// <summary>
        /// 消耗物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="custom_uid"></param>
        /// <param name="item_values"></param>
        /// <param name="result"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<int> _ConsumableUserInventoryItems(string user_uid, string custom_uid,
                            string item_values,
                            NUserInventoryItemsResult result,
                            string reason = "internal")
        {
            item_values = item_values.Trim();
            // 获取道具 (是否有物品发放)
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(item_values);
            if (items == null || items.Length == 0)
            {
                return 0;
            }

            //
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                _logger?.LogError($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) Add Items {item_values} Failed ({reason})");
                return -1;
            }

            List<UserInventoryItem> consumable_items = new List<UserInventoryItem>();
            if( await UserManager.Instance._ConsumableUserInventoryItems(user_uid, item_list, consumable_items, reason) < 0)
            {
                return -1;
            }

            string[] values = consumable_items.Select(v => $"{v.index}|{v.count}|IID{v.iid}").ToArray();
            result.ItemValues = string.Join(",", values);
            return 1;
        }
    }
}

