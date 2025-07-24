using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    [System.Serializable]
    public class NMarketBuyProductRequest
    {
        [JsonPropertyName("index")]
        public int Index = 0;
    }
    [System.Serializable]
    public class NMarketBuyProductResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("id")]
        public string ID = ""; //流程编号
        [JsonPropertyName("index")]
        public int Index;
        [JsonPropertyName("items")]
        public List<string?>? Items = null;
    }
    

    /// <summary>
    /// 
    /// </summary>
    public partial class MarketManager
    {
        protected async Task HandleMarketBuyProduct(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NMarketBuyProductRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            string uid = AMToolkits.Utility.Guid.GeneratorID18N();

            //
            var result = new NMarketBuyProductResponse
            {
                Code = 0,
                ID = uid,
                Index = request.Index,
            };

            // 流水单
            var b_result = await this.BuyProduct(auth_data.id, uid, request.Index);
            if (b_result.Code > 0)
            {
                if (b_result.Items != null) {
                    //result.Items = b_result.Items.Select(v => AMToolkits.Game.ItemUtils.GeneralItemToValue(v)).ToList();
                }
            }

            //
            result.Code = b_result.Code;
            
            //
            await context.ResponseResult(result);
        }
    }
}