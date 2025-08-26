/// 重新命名了排行榜
/// 
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    [System.Serializable]
    public class NLeaderboardRequest
    {
        [JsonPropertyName("type")]
        public int Type = 0;
    }
    [System.Serializable]
    public class NLeaderboardResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("type")]
        public int Type = 0;

        [JsonPropertyName("items")]
        public List<NLeaderboardItem?>? Items = null;
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class LeaderboardManager
    {
        /// <summary>
        /// 列表目前是基于用户私有的，暂时没有公开的
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleLeaderboard(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NLeaderboardRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NLeaderboardResponse
            {
                Code = 0,
                Type = request.Type
            };

            LeaderboardType ranking_type = LeaderboardType.Default;
            LeaderboardSort ranking_sort = LeaderboardSort.Day;
            if (request.Type == 1) // 钻石余额
            {
                ranking_type = LeaderboardType.Paid;
                ranking_sort = LeaderboardSort.Weekly;
            }
            else if (request.Type == 2) // 钻石消费榜
            {
                ranking_type = LeaderboardType.Cost_1;
                ranking_sort = LeaderboardSort.Weekly;
            }
            else if (request.Type == 10)
            {
                ranking_type = LeaderboardType.Rank;
                ranking_sort = LeaderboardSort.Default;
            }


            //
            List<NLeaderboardItem?> items = new List<NLeaderboardItem?>();
            var result_code = await this.GetLeaderboard(ranking_type, ranking_sort, items);
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
