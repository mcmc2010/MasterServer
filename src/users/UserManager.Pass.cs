
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserPassData
    {
        /// <summary>
        /// 当前赛季 通行证
        /// </summary>
        [JsonPropertyName("season_pass_value")]
        public int PassValue = 0;


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
        protected async Task<UserPassData?> GetUserGamePass(string? user_uid)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var data = await DBGetUserPass(user_uid);
            if (data == null)
            {
                return null;
            }
            return data;
        }
    }
}