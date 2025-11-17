
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
        /// <summary>
        /// 这个实际发放金币，不一定和配置表相同
        /// </summary>
        [JsonPropertyName("current_amount")]
        public float? CurrentAmount = null;

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("effects")]
        public string[]? EffectList = null;
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

    [System.Serializable]
    public class PFPaymentFinalResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFCashShopResultItemData? Data = null;
    }


    [System.Serializable]
    public class PFPaymentFinalProductsResponse : AMToolkits.Net.HTTPResponseResult
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
                                    List<string> effect_list,
                                    List<AMToolkits.Game.GeneralItemData> item_list,
                                    float amount,
                                    int currency = (int)AMToolkits.Game.VirtualCurrency.GM,
                                    string reason = "cashshop")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            if (item_list.Count == 0 && effect_list.Count == 0)
            {
                return null;
            }

            string currency_s = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
            if(currency == (int)AMToolkits.Game.VirtualCurrency.GM)
            {
                // nothing
            }
            else if (currency == (int)AMToolkits.Game.VirtualCurrency.GD)
            {
                currency_s = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
            }
            else if (currency == (int)AMToolkits.Game.Currency.CNY)
            {
                currency_s = AMToolkits.Game.CurrencyUtils.CNY;
            }
            else
            {
                return null;
            }


            //
            var response = await this.APICall<PFCashShopBuyProductResponse>("/internal/services/cashshop/buy",
                        new Dictionary<string, object>()
                        {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "nid", nid },
                        { "product_id", product_id },
                        { "currency", currency_s },
                        { "amount", amount },
                        { "items", item_list },
                        { "effects", effect_list },
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

        /// <summary>
        /// 充值：交易
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async Task<PFCashShopResultItemData?> PFPaymentFinal(string user_uid, string playfab_uid,
                                    string nid, //流水单号
                                    TransactionItem transaction,
                                    string reason = "payment")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            // 只有pending,review,approved,rejected
            var response = await this.APICall<PFPaymentFinalResponse>("/internal/services/payment/final",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "nid", nid },
                        { "product_id", transaction.product_id },
                        { "currency", transaction.currency },
                        { "amount", transaction.amount },
                        { "virtual_amount", transaction.virtual_amount },
                        { "virtual_currency", transaction.virtual_currency },
                        { "transaction_code", transaction.result_code ?? "pending" },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFPaymentFinal Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFPaymentFinal Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return null;
            }

            return response.Data;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="playfab_uid"></param>
        /// <param name="nid"></param>
        /// <param name="transaction"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<PFCashShopResultItemData?> PFPaymentFinalProducts(string user_uid, string playfab_uid,
                                    string nid, //流水单号
                                    TransactionItem transaction,
                                    List<string> effect_list,
                                    List<AMToolkits.Game.GeneralItemData> item_list,
                                    string reason = "payment")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            // 只有pending,review,approved,rejected
            var response = await this.APICall<PFPaymentFinalProductsResponse>("/internal/services/payment/final/products",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "nid", nid },
                        { "product_id", transaction.product_id },
                        { "currency", transaction.currency },
                        { "amount", transaction.amount },
                        { "virtual_amount", transaction.virtual_amount },
                        { "virtual_currency", transaction.virtual_currency },
                        { "transaction_code", transaction.result_code ?? "pending" },
                        { "items", item_list },
                        { "effects", effect_list },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFPaymentFinalProducts Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) PFPaymentFinalProducts Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return null;
            }

            return response.Data;
        }
    }
}