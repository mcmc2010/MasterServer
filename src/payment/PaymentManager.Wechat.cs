
using System.Text.Json.Serialization;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class PaymentWechatParams
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

    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        /// <summary>
        /// 随机字符串，不长于32位。该值建议使用随机数算法生成。
        /// </summary>
        /// <returns></returns>
        private string GenerateNonceStr()
        {
            string uid = AMToolkits.Utility.Guid.GeneratorID12();
            return AMToolkits.Hash.SHA256String($"{uid}_{AMToolkits.Utils.GetLongTimestamp()}")[..32];
        }

        private string GenerateTimestamp()
        {
            return $"{AMToolkits.Utils.GetTimestamp()}";
        }
    }
}