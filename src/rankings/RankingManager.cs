
using Microsoft.AspNetCore.Builder;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public enum RankingType
    {
        Default = 0, // 金币或其它游戏币
        Paid = 1,    // 付费排行榜（默认：钻石）
        Cost_0 = 5,  // 花费金币
        Cost_1 = 6,  // 花费钻石
        Rank = 10, // 排位段位
    }

    /// <summary>
    /// 排行榜排序
    /// </summary>
    public enum RankingSort
    {
        Default = 0,
        Minute = 1,
        Hour = 0,
        Day = 2,
        Weekly = 7,
        Monthly = 10,
    }

    #region Ranking
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserRankingItem
    {
        public int uid = 0;
        public string id = ""; //t_user表中的
        public string name = "";
        public int balance = 0;
        public double cost = 0; // 消费累计
        public string currency = "";
        public string rank = "";
        public int ranking_type = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;

        public int status = 0;


        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NUserRankingItem ToNItem()
        {
            return new NUserRankingItem()
            {
                id = this.id,
                name = this.name,
                balance = this.balance,
                cost = this.cost,
                currency = this.currency,
                create_time = this.create_time,
                last_time = this.last_time,
                rank = this.rank,
                ranking_type = this.ranking_type,
            };
        }

    }

    [System.Serializable]
    public class NUserRankingItem
    {
        public string id = ""; //t_user表中的
        public string name = "";
        public int balance = 0;
        public double cost = 0;
        public string currency = "";
        public string rank = "";
        public int ranking_type = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;

    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public partial class RankingManager : AMToolkits.SingletonT<RankingManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static RankingManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public RankingManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[RankingManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.Map("api/ranking/list", HandleRankingList);

        }


        public async System.Threading.Tasks.Task<int> GetRankingList(
                            RankingType ranking_type, RankingSort ranking_sort,
                            List<NUserRankingItem?> items)
        {
            List<UserRankingItem> list = new List<UserRankingItem>();
            if (ranking_type == RankingType.Default && ranking_sort == RankingSort.Day)
            {
                if (await DBGetRankingListWithDaily(ranking_type, list) < 0)
                {
                    return -1;
                }
            }
            else if (ranking_type == RankingType.Paid && ranking_sort == RankingSort.Weekly)
            {
                if (await DBGetRankingListWithWeekly(ranking_type, list) < 0)
                {
                    return -1;
                }
            }
            else if (ranking_type == RankingType.Cost_1 && ranking_sort == RankingSort.Weekly)
            {
                if (await DBGetRankingListWithWeekly(ranking_type, list) < 0)
                {
                    return -1;
                }
            }

            foreach (var v in list)
            {
                items.Add(v.ToNItem());
            }

            return items.Count;
        }

    }
}