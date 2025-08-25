
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
        public int value = 0; // 隐藏评估
        //
        public int cp_value = 0; // 综合能力
        // 对局数量
        public int played_count = 0;
        // 对局获胜数量
        public int played_win_count = 0;
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

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {

    }
}