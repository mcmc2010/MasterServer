
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    [System.Serializable]
    public class NRankingListRequest
    {
        [JsonPropertyName("type")]
        public int Type = 0;
    }
    [System.Serializable]
    public class NRankingListResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("type")]
        public int Type = 0;

        [JsonPropertyName("items")]
        public List<NUserRankingItem?>? Items = null;
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class RankingManager
    {
        /// <summary>
        /// 列表目前是基于用户私有的，暂时没有公开的
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleRankingList(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NRankingListRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NRankingListResponse
            {
                Code = 0,
                Type = request.Type
            };

            RankingType ranking_type = RankingType.Default;
            RankingSort ranking_sort = RankingSort.Day;
            if (request.Type == 1) // 钻石余额
            {
                ranking_type = RankingType.Paid;
                ranking_sort = RankingSort.Weekly;
            }
            else if (request.Type == 2) // 钻石消费榜
            {
                ranking_type = RankingType.Cost_1;
                ranking_sort = RankingSort.Weekly;
            }


            //
            List<NUserRankingItem?> items = new List<NUserRankingItem?>();
            var result_code = await this.GetRankingList(ranking_type, ranking_sort, items);
            if (result_code >= 0)
            {
                result.Items = items;
            }

            //
            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }
    }
}