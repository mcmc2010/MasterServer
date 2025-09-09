
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

using Logger;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class PFNCashShopItemData : PFNItemData
    {
        /// <summary>
        /// 流水单号
        /// </summary>
        [JsonPropertyName("nid")]
        public string NID = "";
        [JsonPropertyName("product_id")]
        public string ProductID = "";

        [JsonPropertyName("currency")]
        public string VirtualCurrency = "";
        [JsonPropertyName("balance")]
        public float Balance = 0.0f;
        [JsonPropertyName("amount")]
        public float Amount = 0.0f;

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("current_currency")]
        public string CurrentVirtualCurrency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
        [JsonPropertyName("current_balance")]
        public float? CurrentBalance = null;
    }

    [System.Serializable]
    public class PFCashShopResultItemData : PFResultBaseData
    {
        public PFNCashShopItemData? Data = null;
    }

    [System.Serializable]
    public class PFCashShopBuyProductResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFCashShopResultItemData? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PlayFabService
    {
        /// <summary>
        /// 购买物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async Task<PFCashShopResultItemData?> PFCashShopBuyProduct(string user_uid, string playfab_uid,
                                    string nid, //流水单号
                                    string product_id,
                                    List<AMToolkits.Game.GeneralItemData> list,
                                    float amount,
                                    AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GM,
                                    string reason = "cashshop")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            if (list.Count == 0)
            {
                return null;
            }

            var response = await this.APICall<PFCashShopBuyProductResponse>("/internal/services/cashshop/buy",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "nid", nid },
                        { "product_id", product_id },
                        { "currency", currency == AMToolkits.Game.VirtualCurrency.GD ?
                                    AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT:
                                    AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT},
                        { "amount", amount },
                        { "items", list },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFCashShopBuyProduct Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFCashShopBuyProduct Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return response.Data;
            }

            return response.Data;
        }
    }
}