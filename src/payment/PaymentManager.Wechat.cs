
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AMToolkits.Extensions;
using AMToolkits.Net;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class WechatParams
    {
        [JsonPropertyName("appId")]
        public string AppId = "";

        [JsonPropertyName("partnerId")]
        public string PartnerId = "";

        [JsonPropertyName("prepayId")]
        public string PrepayId = "";

        [JsonPropertyName("packageValue")]
        public string PackageValue = "Sign=WXPay";

        [JsonPropertyName("nonceStr")]
        public string NonceStr = "";

        [JsonPropertyName("timeStamp")]
        public string Timestamp = "";

        [JsonPropertyName("sign")]
        public string Sign = "";
    }

    [System.Serializable]
    public class WechatPrepayDataResponse : AMToolkits.Net.HTTPResponseResult
    {        
        [JsonPropertyName("prepay_id")]
        public string PrepayId = "";
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        private HTTPClientFactory? _wx_client_factory = null;

        /// <summary>
        /// 随机字符串，不长于32位。该值建议使用随机数算法生成。
        /// </summary>
        /// <returns></returns>
        private string GenerateNonceString()
        {
            string uid = AMToolkits.Utility.Guid.GeneratorID12();
            return AMToolkits.Hash.SHA256String($"{uid}_{AMToolkits.Utils.GetLongTimestamp()}")[..32];
        }

        private string GenerateTimestamp()
        {
            return $"{AMToolkits.Utils.GetTimestamp()}";
        }

        private string ToWechatBody(Dictionary<string, object?> payload)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(payload);
            }
            catch(Exception ex)
            {
                _logger?.LogError("(Wechat Proxy) JSON : " + ex.Message);
                return "";
            }
        }

        private async Task _WechatPrepay(string user_uid, TransactionItem transaction)
        {
            var goods_detail = new List<object>();
            var goods = new Dictionary<string, object>()
            {
                    // 商户侧商品编码: 由半角的大小写字母、数字、中划线、下划线中的一种或几种组成。
                    { "merchant_goods_id", transaction.product_id },
                    { "goods_name", transaction.name }, // 商品的实际名称
                    { "quantity", transaction.count },   // 用户购买的数量
                    // 商品单价: 单位为：分
                    { "unit_price", System.Math.Min((transaction.price / transaction.count) * 100, 1) }
            };
            goods_detail.Add(goods);

            //
            var payload = new Dictionary<string, object?>()
                {
                    { "notify_url", _settings.Wechat.NotifyURL },
                    //
                    { "mchid", _settings.Wechat.MerchantID },
                    { "description", transaction.name },
                    { "out_trade_no", transaction.order_id },
                    { "amount", new Dictionary<string, object>()
                        {
                            { "total", transaction.amount },
                            { "currency", transaction.currency }
                        }
                    },
                    { "detail", new Dictionary<string, object>()
                        {
                            // 商户侧一张小票订单可能被分多次支付，订单原价用于记录整张小票的交易金额。
                            { "cost_price", transaction.price },
                            { "goods_detail", goods_detail },
                        }
                    }
                };

            var body = this.ToWechatBody(payload);
            if(body.IsNullOrWhiteSpace())
            {
                return;
            }

            string nonce_str = GenerateNonceString();
            string timestamp = GenerateTimestamp();

            var sign_data = $"WECHATPAY2-SHA256-RSA2048" +
                    $" mchid=\"{_settings.Wechat.MerchantID}\",nonce_str=\"{nonce_str}\",signature=\"\",timestamp=\"{timestamp}\"";
            Dictionary<string, object> headers = new Dictionary<string, object>();
            headers.Add("Authorization", sign_data);

            //
            var response = await WechatOpenAPIPost<WechatPrepayDataResponse>("/v3/pay/transactions/app", payload, null, headers);
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (WechatPrepay) : {response?.Message ?? _wx_client_factory?.LastError?.Message }");
                return ;
            }
        }
    }
}