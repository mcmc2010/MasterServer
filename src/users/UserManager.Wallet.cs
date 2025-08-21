
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    public partial class UserManager
    {

        #region Server Internal
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async Task<AMToolkits.Game.WalletData?> _GetWalletData(string user_uid)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var user = this.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return null;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFGetWalletData(user_uid, user.CustomID);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }

            AMToolkits.Game.WalletData wallet = new AMToolkits.Game.WalletData();
            // 
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS, out var gems) )
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gems = System.Convert.ToSingle(gems);
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD, out var gold) )
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gold = System.Convert.ToSingle(gold);
            return wallet;
        }

        /// 物品更新
        /// </summary>
        public async Task<Dictionary<string, object?>?> _UpdateVirtualCurrency(string user_uid,
                            float amount = 0.0f,
                            AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GD,
                            string reason = "")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var user = this.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return null;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, user.CustomID, amount, currency, reason);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (UpdateVirtualCurrency) (User:{user_uid}) Amount: {amount} {currency} Failed");
                return null;
            }

            // 更新成功，更新本地数据库
            // 暂时不处理

            return result;
        }
        #endregion
    }
}