
using Logger;
using AMToolkits.Extensions;

namespace Server
{
    [System.Serializable]
    public class PFGetWalletDataResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFResultData? Data = null;
    }

    [System.Serializable]
    public class PFUpdateVirtualCurrencyResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFResultData? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PlayFabService
    {
        /// <summary>
        /// 获取玩家钱包
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, object?>?> PFGetWalletData(string user_uid, string playfab_uid)
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            //
            var response = await this.APICall<PFUpdateVirtualCurrencyResponse>("/internal/services/user/wallet/data",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) GetWalletData Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }


            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) GetWalletDat Failed: ({playfab_uid}) " +
                                  $" [{response.Data?.Result}:{response.Data?.Error} {response.Data?.Description ?? ""}]");
                return null;
            }

            Dictionary<string, object?>? data = response.Data?.Data.ToDictionaryObject();
            return data;
        }

        /// <summary>
        /// 更新玩家钱币
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, object?>?> PFUpdateVirtualCurrency(string user_uid, string playfab_uid,
                                    float amount = 0.0f,
                                    AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GD,
                                    string reason = "")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            if (amount == 0.0f)
            {
                return null;
            }

            //
            var response = await this.APICall<PFUpdateVirtualCurrencyResponse>("/internal/services/user/wallet/update",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "currency", currency == AMToolkits.Game.VirtualCurrency.GD ?
                                    AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT:
                                    AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT},
                        { "amount", amount },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) UpdateVirtualCurrency Failed: ({playfab_uid}) Amount : {amount:F2}({currency}) {_client_factory?.LastError?.Message}");
                return null;
            }


            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) UpdateVirtualCurrency Failed: ({playfab_uid}) Amount : {amount:F2}({currency})" +
                                  $" [{response.Data?.Result}:{response.Data?.Error} {response.Data?.Description ?? ""}]");
                return null;
            }

            Dictionary<string, object?>? data = response.Data?.Data.ToDictionaryObject();
            return data;
        }
    }
}