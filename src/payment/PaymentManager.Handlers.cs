using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    [System.Serializable]
    public class NTransactionsV1Request
    {
        [JsonPropertyName("type")]
        public int Type = 1; // 1:默认-支付

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("name")]
        public string Name = "";
        [JsonPropertyName("product_id")]
        public string ProductID = "";
        [JsonPropertyName("count")]
        public int Count = 1; // 默认是1

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency = AMToolkits.Game.CurrencyUtils.CNY;
        [JsonPropertyName("amount")]
        public float Amount = 0.00f;
        [JsonPropertyName("price")]
        public float Price = 0.00f; // 原价
    }

    [System.Serializable]
    public class NTransactionsV1Response
    {
        [JsonPropertyName("code")]
        public int Code;


        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("order_id")]
        public string OrderID = ""; //订单号

        [JsonPropertyName("type")]
        public int Type = 1; // 1:默认-支付

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("name")]
        public string Name = "";
        [JsonPropertyName("product_id")]
        public string ProductID = "";
        [JsonPropertyName("count")]
        public int Count = 1; // 默认是1

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency = AMToolkits.Game.CurrencyUtils.CNY;
        [JsonPropertyName("amount")]
        public float Amount = 0.00f;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleTransactionV1(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NTransactionsV1Request>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            // 流水单
            string uid = AMToolkits.Utility.Guid.GeneratorID18N();
            string order_id = AMToolkits.Utility.Guid.GeneratorID10();

            //
            var result = new NTransactionsV1Response
            {
                Code = 0,
                ID = uid,

                //
                ProductID = request.ProductID,
                Name = request.Name.Trim(),
                Count = request.Count,
            };

            var transaction = new TransactionItem()
            {
                id = uid,
                order_id = order_id,
                name = request.Name.Trim(),
                type = request.Type,
                sub_type = 0,

                product_id = request.ProductID,
                count = request.Count,
                amount = request.Amount,
                price = request.Price,
                fee = 0.00f,
                currency = AMToolkits.Game.CurrencyUtils.CNY,

                user_id = auth_data.id,
                custom_id = "",

                channel = "WEB",
                payment_method = null,

                create_time = DateTime.UtcNow,
                update_time = DateTime.UtcNow,
                complete_time = null,


            };

            // 流水单
            var result_code = await this.StartTransaction_V1(auth_data.id, transaction);
            if (result_code > 0)
            {
                result.OrderID = transaction.order_id;
                result.Type = transaction.type;

                result.Name = transaction.name;

                result.Currency = transaction.currency;
                result.Amount = (float)transaction.amount;
            }

            //
            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }
    }
}