
using AMToolkits.Extensions;
using AMToolkits.Game;

using Logger;

namespace Server
{
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

            return result;
        }

    }
}

