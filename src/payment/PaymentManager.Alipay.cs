
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using AMToolkits.Net;
using Logger;



namespace Server
{
    /// <summary>
    /// biz_content
    /// </summary>
    [System.Serializable]
    public class AlipayTransactionModel
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("out_trade_no")]
        public string OutTradeNo = "";
        [JsonPropertyName("trade_no")]
        public string TradeNo = "";

    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class AlipayGetTransactionData
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("code")]
        public string Code = "";
        [JsonPropertyName("sub_code")]
        public string SubCode = "";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("msg")]
        public string Message = "";
        [JsonPropertyName("sub_msg")]
        public string SubMessage = "";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("out_trade_no")]
        public string OutTradeNo = "";
        [JsonPropertyName("trade_no")]
        public string TradeNo = "";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("buyer_logon_id")]
        public string BuyerID = "";
        [JsonPropertyName("buyer_pay_amount")]
        public string BuyerPayAmount = "";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("invoice_amount")]
        public string InvoiceAmount = "";

        [JsonPropertyName("total_amount")]
        public string TotalAmount = "";
        
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("trade_status")]
        public string Status = "";
    }

    [System.Serializable]
    public class AlipayGetTransactionDataResponse : AMToolkits.Net.HTTPResponseResult
    {
        [JsonPropertyName("alipay_trade_query_response")]
        public AlipayGetTransactionData? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        /// <summary>
        /// 
        /// </summary>
        public const string RESULT_REASON_SUCCESS = "SUCCESS";
        /// <summary>
        /// 交易不存在
        /// </summary>
        public const string RESULT_REASON_TRADE_NOT_EXIST = "TRADE_NOT_EXIST";


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string? ToJsonFromObject(object? o)
        {
            if (o == null)
            {
                return null;
            }

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(o,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                IgnoreReadOnlyFields = true,
                                IncludeFields = true,
                                // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                            });
                return json;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return null;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public async Task<AlipayGetTransactionDataResponse?> AlipayGetTransactionData(string user_uid, TransactionItem transaction)
        {
            _logger?.LogError($"{TAGName} (AlipayGetTransactionData) : ({user_uid}) TradeQuery {transaction.order_id}");

            //
            AlipayTransactionModel model = new AlipayTransactionModel()
            {
                OutTradeNo = transaction.order_id,
            };
            string biz_content = ToJsonFromObject(model) ?? "";

            //
            var response = await this.AlipayOpenAPIGet<AlipayGetTransactionDataResponse>("",
                    new Dictionary<string, object>()
                    {
                        {"biz_content", biz_content}
                    });
            if (response == null || response.Data == null)
            {
                _logger?.LogError($"{TAGName} (AlipayGetTransactionData) : {response?.Message}");
                return null;
            }

            response.Data.Code = response.Data.Code.Trim().ToUpper();
            response.Message = $"{response.Data.Message}";
            response.Status = $"{response.Data.SubCode}";

            if (response.Data.Code != "10000")
            {
                response.Code = -System.Convert.ToInt32(response.Data.Code);

                if (response.Status == "ACQ.TRADE_NOT_EXIST")
                {
                    response.Status = RESULT_REASON_TRADE_NOT_EXIST;
                }
                _logger?.LogError($"{TAGName} (AlipayGetTransactionData) : {transaction.order_id} - {response.Data.Code} - {response.Data.Message}");
                _logger?.LogError($"{TAGName} (AlipayGetTransactionData) : (TRACE) {transaction.order_id} - {response.Data.SubMessage} ({response.Data.SubCode})");

            }
            else
            {
                response.Code = 1;
                if (response.Data.Status == "TRADE_SUCCESS")
                {
                    response.Status = RESULT_REASON_SUCCESS;
                }
                else if(!response.Data.Status.IsNullOrWhiteSpace())
                {
                    response.Status = response.Data.Status;
                }
            }

            return response;
        }

    }
}