

using System.Text.Json.Serialization;

using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserRankData
    {
        /// <summary>
        /// 当前赛季 段位
        /// </summary>
        [JsonPropertyName("rank_level")]
        public int RankLevel = 0;

        /// <summary>
        /// 当前赛季 段位
        /// </summary>
        [JsonPropertyName("rank_value")]
        public int RankValue = 0;

        /// <summary>
        /// 上赛季 段位
        /// </summary>
        [JsonPropertyName("last_rank_level")]
        public int LastRankLevel = 0;

        /// <summary>
        /// 上赛季 段位
        /// </summary>
        [JsonPropertyName("last_rank_value")]
        public int LastRankValue = 0;

        /// <summary>
        /// 大师印记
        /// </summary>
        [JsonPropertyName("challenger_reals")]
        public int ChallengerReals = 0;

        /// <summary>
        /// 玩家参与的赛季
        /// </summary>
        [JsonPropertyName("season")]
        public int Season = 1;

        /// <summary>
        /// 玩家参与的赛季时间
        /// </summary>
        [JsonPropertyName("season_time")]
        public DateTime? SeasonTime = null;


    }

    [System.Serializable]
    public class UserRankDataExtend : UserRankData
    {

    }


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        /// <summary>
        /// 获取用户段位
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task<UserRankDataExtend?> GetUserRank(string? user_uid)
        {
            return null;
        }
    }
}