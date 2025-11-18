
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserGamePassData
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
        #region Server Internal
        public int _GetUserGamePassLevel(int pass_value)
        {
            int level = 0;
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TBattlePass>();
            if (template_data == null)
            {
                return level;
            }

            int total = 0;
            foreach(var item in template_data)
            {
                total += item.Exp;
                if(pass_value < total)
                {
                    level = item.Level;
                    break;
                }
            }
            return level;
        }
        

        public async Task<int> _GetUserGamePass(string user_uid,
                            List<UserGamePassData> pass_list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            if (await DBGetUserGamePass(user_uid, pass_list) < 0)
            {
                return -1;
            }

            return pass_list.Count;
        }
        #endregion

        /// <summary>
        /// 获取用户段位
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task<int> GetUserGamePass(string? user_uid, List<UserGamePassData> pass_list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if(await DBGetUserGamePass(user_uid, pass_list) < 0)
            {
                return -1;
            }
            return 1;
        }
    }
}