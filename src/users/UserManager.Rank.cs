

using System.Text.Json.Serialization;
using AMToolkits.Extensions;
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

        [JsonPropertyName("rank_level_best")]
        public int RankLevelBest = 0;

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

        /// <summary>
        /// 玩家综合实力值
        /// </summary>
        [JsonPropertyName("cp_value")]
        public int CPValue = 100;
    }

    /// <summary>
    /// 包含游戏数据的扩展数据
    /// </summary>
    [System.Serializable]
    public class UserRankDataExtend : UserRankData
    {
        [JsonPropertyName("rank_score")]
        public int RankScore = 0;

        [JsonPropertyName("played_count")]
        public int PlayedCount = 0;
        [JsonPropertyName("played_win_count")]
        public int PlayedWinCount = 0;

        [JsonPropertyName("winning_streak_count")]
        public int WinningStreakCount = 0;
        [JsonPropertyName("winning_streak_highest")]
        public int WinningStreakHighest = 0;

        [JsonPropertyName("season_played_count")]
        public int SeasonPlayedCount = 0;
        [JsonPropertyName("season_played_win_count")]
        public int SeasonPlayedWinCount = 0;

        [JsonPropertyName("season_winning_streak_count")]
        public int SeasonWinningStreakCount = 0;
        [JsonPropertyName("season_winning_streak_highest")]
        public int SeasonWinningStreakHighest = 0;
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
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var data = await DBGetUserRank(user_uid);
            if (data == null)
            {
                return null;
            }
            return data;
        }
    }
}