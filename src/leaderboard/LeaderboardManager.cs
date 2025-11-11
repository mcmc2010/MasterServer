/// 重新命名了排行榜
/// 
/// 

using Microsoft.AspNetCore.Builder;
using AMToolkits.Extensions;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public enum LeaderboardType
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
    public enum LeaderboardSort
    {
        Default = 0,
        Minute = 1,
        Hour = 0,
        Day = 2,
        Weekly = 7,
        Monthly = 10,
    }

    #region Leaderboard
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class LeaderboardItem
    {
        public int uid = 0;
        public string id = ""; //t_user表中的
        public string name = "";

        /// <summary>
        /// 
        /// </summary>
        public string avatar_url = "";
        
        public int balance = 0;
        public double cost = 0; // 消费累计
        public string currency = "";
        public string rank = "";

        public string items = "";

        public int type = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;

        public int status = 0;


        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NLeaderboardItem ToNItem()
        {
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(this.items);
            return new NLeaderboardItem()
            {
                id = this.id,
                name = this.name,
                avatar_url = this.avatar_url,
                balance = this.balance,
                cost = this.cost,
                currency = this.currency,
                create_time = this.create_time,
                last_time = this.last_time,
                rank = this.rank,
                items = items,
                type = this.type,
            };
        }

    }

    [System.Serializable]
    public class NLeaderboardItem
    {
        public string id = ""; //t_user表中的
        public string name = "";
        /// <summary>
        /// 
        /// </summary>
        public string avatar_url = "";

        /// <summary>
        /// 
        /// </summary>
        public int balance = 0;
        public double cost = 0;
        public string currency = "";
        public string rank = "";

        public AMToolkits.Game.GeneralItemData[]? items = null;
        public int type = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;

    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public partial class LeaderboardManager : AMToolkits.SingletonT<LeaderboardManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static LeaderboardManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public LeaderboardManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[LeaderboardManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.Map("api/leaderboard/list", HandleLeaderboard);

        }




        public async System.Threading.Tasks.Task<int> GetLeaderboard(
                            LeaderboardType ranking_type, LeaderboardSort ranking_sort,
                            List<NLeaderboardItem?> items)
        {
            List<LeaderboardItem> list = new List<LeaderboardItem>();
            if (ranking_type == LeaderboardType.Default && ranking_sort == LeaderboardSort.Day)
            {
                if (await DBGetLeaderboardWithDaily(ranking_type, list) < 0)
                {
                    return -1;
                }
            }
            else if (ranking_type == LeaderboardType.Paid && ranking_sort == LeaderboardSort.Weekly)
            {
                if (await DBGetLeaderboardWithWeekly(ranking_type, list) < 0)
                {
                    return -1;
                }
            }
            else if (ranking_type == LeaderboardType.Cost_1 && ranking_sort == LeaderboardSort.Weekly)
            {
                if (await DBGetLeaderboardWithWeekly(ranking_type, list) < 0)
                {
                    return -1;
                }
            }
            else if (ranking_type == LeaderboardType.Rank)
            {
                if (await DBGetLeaderboardWithDaily(ranking_type, list) < 0)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="name"></param>
        /// <param name="currency"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        public async Task<int> _UpdateLeaderboardRecord(string user_uid, string? name,
                            string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT,
                            int balance = 0)
        {
            int result_code = await DBUpdateLeaderboardRecord(user_uid, name, currency, balance);
            if (result_code < 0)
            {
                return -1;
            }


            UserProfileExtend profile = new UserProfileExtend();
            if (await UserManager.Instance._GetUserProfile(user_uid, profile) < 0)
            {
                return -1;
            }

            await _UpdateLeaderboardUserProfile(user_uid, profile);

            await _UpdateLeaderboardUserInventoryItems(user_uid);
            return result_code;
        }

        public async Task<int> _UpdateLeaderboardRecord(string user_uid, string? name,
                            string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT,
                            double cost = 0.00)
        {
            int result_code = await DBUpdateLeaderboardRecord(user_uid, name, currency, cost);
            if (result_code < 0)
            {
                return -1;
            }


            UserProfileExtend profile = new UserProfileExtend();
            if (await UserManager.Instance._GetUserProfile(user_uid, profile) < 0)
            {
                return -1;
            }

            await _UpdateLeaderboardUserProfile(user_uid, profile);


            await _UpdateLeaderboardUserInventoryItems(user_uid);
            return result_code;
        }

        public async Task<int> _UpdateLeaderboardRecord(string user_uid, string? name,
                            int rank_level, int rank_value = 0)
        {
            int result_code = await DBUpdateLeaderboardRecord(user_uid, name, rank_level, rank_value);
            if (result_code < 0)
            {
                return -1;
            }

            UserProfileExtend profile = new UserProfileExtend();
            if (await UserManager.Instance._GetUserProfile(user_uid, profile) < 0)
            {
                return -1;
            }

            await _UpdateLeaderboardUserProfile(user_uid, profile);

            await _UpdateLeaderboardUserInventoryItems(user_uid);
            return result_code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected async Task _UpdateLeaderboardUserProfile(string user_uid, UserProfileExtend profile)
        {
            if (user_uid != profile.UID)
            {
                return;
            }

            await DBUpdateLeaderboardRecord(user_uid, profile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task _UpdateLeaderboardUserInventoryItems(string user_uid)
        {
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if (await UserManager.Instance._GetUserInventoryItems(user_uid, list, AMToolkits.Game.ItemType.Equipment, true) < 0)
            {

            }

            // 目前只有装备，以后可能有皮肤，套装
            var using_item = list.FirstOrDefault();
            if (using_item != null)
            {
                await DBUpdateLeaderboardRecord(user_uid, using_item.ToNItem());
            }
        }

    }
}