
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    public partial class UserManager
    {

        #region Server Internal
        /// 物品更新
        /// </summary>
        public async Task<int> _CashshopBuyProduct(string user_uid, string custom_uid,
                            PFNCashShopItemData? data)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (data == null || data.PlayFabUID != custom_uid)
            {
                return -1;
            }

            UserManager.Instance._UpdateWalletData(user_uid, data);

            // 保存购买记录
            if (await _DBAddCashshopItems(user_uid, data) < 0)
            {
                return -1;
            }

            // 如果是游戏币
            if (data.ItemList == null)
            {
                
            }
            // 如果包含物品，添加物品
            else
            {
                var list = data.ItemList.ToList();
                if (list == null || list.Count == 0)
                {
                    _logger?.LogWarning($"{TAGName} (BuyProduct) (User:{user_uid}) {data.NID} - {data.ProductID} Amount: {data.Amount} {AMToolkits.Game.VirtualCurrency.GM} Not Items");
                    return 0;
                }

                // 增加物品
                if (await _DBAddUserInventoryItems(user_uid, list) < 0)
                {
                    return -1;
                }
                return list.Count;
            }
            return 1;
        }
        #endregion
    }
}

