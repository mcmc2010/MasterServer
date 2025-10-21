
using AMToolkits.Extensions;
using Game;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserHOLData
    {
        public int uid = 0;
        public string id = "";
        public string name = ""; //t_user表中的
        public int level = 0;
        public long experience = 0;
        public int value = 0; // 隐藏评估
        //
        public int cp_value = 0; // 综合能力
        // 对局数量
        public int played_count = 0;
        // 对局获胜数量
        public int played_win_count = 0;
        public int winning_streak_count = 0;
        public int winning_streak_highest = 0;
        public DateTime? create_time = null;
        public DateTime? last_time = null;

        // ranking
        public int season = 1;
        public DateTime? season_time = null;
        // 上赛季
        public int last_rank_level = 1000;
        public int last_rank_value = 0;
        // 本赛季
        public int rank_level = 1000;
        public int rank_value = 0;
        //
        public int challenger_reals = 0;

        public int status = 0;

    }

    [System.Serializable]
    public class UserExperienceResult
    {
        public int level = 0;
        public int experience = 0;
        public int experience_max = 0;
        public int next_level = 0;
        public int next_experience = 0;
        public int next_experience_max = 0;

        public float value = 0.0f;
        public float ratio = 1.0f;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="experience"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<int> _AddUserExperience(string? user_uid, float experience, UserExperienceResult result)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            var templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TPlayerLevel>();
            var template_item_list = templates_data?.ToList()?.OrderBy(t => t.Level)?.ToList();
            if (template_item_list == null)
            {
                return 0;
            }

            // 1:
            UserHOLData? hol_data = null;
            this.DBGetHOLData(user_uid, out hol_data);
            if (hol_data == null)
            {
                return 0;
            }

            // 2:
            int level = hol_data.level;
            float current = hol_data.experience + experience;

            var template_item = template_item_list.FirstOrDefault(v => v.Level == hol_data.level);
            if (template_item == null)
            {
                template_item = template_item_list[template_item_list.Count - 1];
            }

            result.level = hol_data.level;
            result.experience = (int)hol_data.experience;
            result.experience_max = template_item.Exp;
            result.value = experience;

            TPlayerLevel? template_item_next = null;

            int total = 0;
            // 经验是被扣除，需要降等级
            if (current < hol_data.experience)
            {
                // 从高等级向低等级查找：找到第一个经验要求 <= 当前经验的模板
                for (int i = level - 1; i >= 0; i--)
                {
                    if (current >= 0.0f)
                    {
                        template_item_next = template_item_list[i];
                        break;
                    }
                    else if (current < 0.0f && i - 1 >= 0)
                    {
                        total += template_item_list[i - 1].Exp;
                        if (total + current >= 0.0f)
                        {
                            template_item_next = template_item_list[i - 1];
                            break;
                        }
                    }


                }
                
                if(template_item_next?.Level < level)
                {
                    current = (total + current);
                }
            }
            else
            {
                template_item_next = template_item_list.FirstOrDefault(v =>
                {
                    if (v.Level < level) { return false; }

                    total += v.Exp;
                    return total >= current;
                });
                
                if(template_item_next != null)
                {
                    current = template_item_next.Exp - (total - current);
                }
            }


            if (template_item_next != null)
            {
                level = template_item_next.Level;
                if (current < 0.0f)
                {
                    current = 0.0f;
                }
            }

            hol_data.level = level;
            hol_data.experience = (int)current;

            if (this.DBUpdateHOLData(user_uid, hol_data) < 0)
            {
                return -1;
            }


            result.next_level = level;
            result.next_experience = (int)current;
            result.next_experience_max = template_item_next?.Exp ?? template_item.Exp;

            return 1;
        }
        #endregion

    }
}