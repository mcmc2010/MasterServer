using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    [System.Serializable]
    public class NTransactionV1Request
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

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("method")]
        public int method = (int)PaymentMethod.None;
    }

    [System.Serializable]
    public class NTransactionV1Response
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


    [System.Serializable]
    public class NTransactionFinalV1Request
    {
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("order_id")]
        public string OrderID = ""; //订单号

        [JsonPropertyName("reason")]
        public string Reason = "completed"; //

        [JsonPropertyName("method")]
        public int method = (int)PaymentMethod.None;
    }

    [System.Serializable]
    public class NTransactionFinalV1Response
    {
        [JsonPropertyName("code")]
        public int Code;


        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("order_id")]
        public string OrderID = ""; //订单号
    }

    [System.Serializable]
    public class NCheckTransactionV1Request
    {
       [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("order_id")]
        public string OrderID = ""; //订单号

        [JsonPropertyName("data")]
        public string Data = ""; //
    }

    [System.Serializable]
    public class NCheckTransactionV1Response
    {
        [JsonPropertyName("code")]
        public int Code;


        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("order_id")]
        public string OrderID = ""; //订单号

        [JsonPropertyName("data")]
        public string Data = ""; //
        
        [JsonPropertyName("sign_data")]
        public string SignData = ""; //
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
            var request = await context.Request.JsonBodyAsync<NTransactionV1Request>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            // 流水单
            string uid = AMToolkits.Utility.Guid.GeneratorID18N();
            string order_id = AMToolkits.Utility.Guid.GeneratorID10();

            //
            var result = new NTransactionV1Response
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

            if (request.method == (int)PaymentMethod.Alipay)
            {
                transaction.payment_method = PaymentMethod.Alipay.ToString().ToLower();
            }
            else if (request.method == (int)PaymentMethod.Wechat)
            {
                transaction.payment_method = PaymentMethod.Wechat.ToString().ToLower();
            }
            else
            {
                transaction.payment_method = "none";
            }

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


        /// <summary>
        /// 完成订单更新
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleTransactionFinalV1(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NTransactionFinalV1Request>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            // 流水单
            var result = new NTransactionFinalV1Response
            {
                Code = 0,
                ID = request.ID,
                OrderID = request.OrderID,
            };

            var transaction = new TransactionItem()
            {
                id = request.ID,
                order_id = request.OrderID,
                name = "",
                type = 1,
                sub_type = 0,

                product_id = "",
                count = 1,
                amount = 0.00,
                price = 0.00,
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

            if (request.method == (int)PaymentMethod.Alipay)
            {
                transaction.payment_method = PaymentMethod.Alipay.ToString().ToLower();
            }
            else if (request.method == (int)PaymentMethod.Wechat)
            {
                transaction.payment_method = PaymentMethod.Wechat.ToString().ToLower();
            }
            else
            {
                transaction.payment_method = "none";
            }

            // 流水单
            var result_code = await this.FinalTransaction_V1(auth_data.id, transaction, request.Reason);
            if (result_code > 0)
            {
                result.ID = transaction.id;
                result.OrderID = transaction.order_id;

                // 临时增加：正常完成的订单修改为pending

                await this._PendingTransaction(auth_data.id, transaction);
            }

            //
            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }

        protected async Task HandleCheckTransactionV1(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NCheckTransactionV1Request>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            // 流水单
            var result = new NCheckTransactionV1Response
            {
                Code = 0,
                ID = request.ID,
                OrderID = request.OrderID,
                Data = request.Data
            };

            var transaction = new TransactionItem()
            {
                id = request.ID,
                order_id = request.OrderID,
                name = "",
                type = 1,
                sub_type = 0,

                product_id = "",
                count = 1,
                amount = 0.00,
                price = 0.00,
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
            var result_data = await this.CheckTransaction_V1(auth_data.id, transaction, request.Data);
            if (result_data != null)
            {
                result.ID = transaction.id;
                result.OrderID = transaction.order_id;
                result.Code = 1;

                result.SignData = result_data;
            }
            else
            {
                result.Code = -1;
            }

            //
            await context.ResponseResult(result);
        }
    }
}