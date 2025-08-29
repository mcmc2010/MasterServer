
using System.Text.Json.Serialization;

using AMToolkits.Extensions;
using AMToolkits.Utility;
using Logger;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public class UserBaseData
    {
        public string server_uid = "";
        public string client_uid = "";
        public string passphrase = "";
        public string token = "";
        public DateTime datetime = DateTime.Now;
        public string device = "";

        public int privilege_level = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public class UserAuthenticationData : UserBaseData
    {
        public string? name = null;

        ///
        public string custom_id = "";
        ///
        public string jwt_token = "";
    }


    #region User Profile

    [System.Serializable]
    public class UserProfile
    {

        [JsonPropertyName("uid")]
        public string UID = "";

        [JsonPropertyName("name")]
        public string Name = "";

        [JsonPropertyName("gender")]
        public int Gender = (int)UserGender.Female;

        [JsonPropertyName("region")]
        public string Region = "";

        /// <summary>
        /// 目前头像只能选择已有图像
        /// </summary>
        [JsonPropertyName("avatar_id")]
        public int AvatarID = 0;
        /// <summary>
        /// 未使用
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl = "";

        public void InitFromDB(DBUserProfile? profile)
        {
            this.AvatarID = profile?.AvatarID ?? 0;
            this.AvatarUrl = "";
            this.Gender = profile?.Gender ?? (int)UserGender.Female;
            this.Region = profile?.Region ?? "";

            this.Name = profile?.Name ?? "";
        }
    }

    [System.Serializable]
    public class UserProfileExtend : UserProfile
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
    #endregion


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_data"></param>
        /// <returns></returns>
        protected async Task<int> AuthenticationAndInitUser(UserAuthenticationData user_data)
        {
            // DB 验证
            int result_code = this.DBAuthUser(user_data);
            if (result_code < 0)
            {
                user_data.passphrase = "";
                user_data.token = "";
                return result_code;
            }

            // 2: HOL
            result_code = this.DBInitHOL(user_data);
            if (result_code < 0)
            {
                return result_code;
            }

            // 3: JWT
            string hash = "";
            if (_config?.JWTEnabled == true)
            {
                hash = JWTAuth.JWTSignData(new Dictionary<string, object>() {
                    { "uid", user_data.client_uid },
                    { "server_uid", user_data.server_uid },
                    { "token", user_data.token }
                }, _config?.JWTSecretKey ?? "", _config?.JWTExpired ?? -1);
                // JWT 认证失败
                if (hash.Length == 0)
                {
                    //result_code = -100;
                }
            }
            user_data.jwt_token = hash;

            // 4:
            // Add User To Manager
            var user = this.AllocT<UserBase>();
            user.ID = user_data.server_uid;
            user.ClientID = user_data.client_uid;
            user.AccessToken = user_data.token;
            user.Passphrase = user_data.passphrase;

            user.PrivilegeLevel = user_data.privilege_level;

            // 自定义ID，这里目前是PlayFabId
            user.CustomID = user_data.custom_id;

            this.AddUser(user);

            //
            _logger?.Log($"(User) Auth User (ClientUID:{user_data.client_uid} - {user_data.server_uid}) Token:{user_data.token} Result: {result_code}");
            if (user_data.privilege_level >= 7)
            {
                _logger?.LogWarning($"(UserAdmin) (ClientUID:{user_data.client_uid} - {user_data.server_uid}) Level:{user_data.privilege_level}");
            }

            // 5.
            var items = await PlayFabService.Instance.PFGetInventoryItems(user_data.server_uid, user_data.custom_id, "user_init");
            if (items != null && items.Data?.ItemList != null)
            {
                int result_count = 0;
                if ((result_count = await this._DBUpdateUserInventoryItems(user_data.server_uid, [.. items.Data.ItemList])) < 0)
                {
                    _logger?.LogWarning($"(User:{user_data.server_uid}) UpdateInventoryItems Failed (Count: {items.Data.ItemList.Length}) Result:{result_count}");
                }
            }
            else
            {
                _logger?.LogWarning($"(User:{user_data.server_uid}) GetInventoryItems Failed");
            }

            var wallet = await this._GetWalletData(user_data.server_uid);
            if (wallet != null)
            {
                // Leaderboard
                // 钻石大于5000加入排行榜
                if (wallet.integer_gems > GameSettingsInstance.Settings.Leaderboard.GemsLimitMin)
                {
                    await LeaderboardManager.Instance._UpdateLeaderboardRecord(user_data.server_uid, user_data.name, AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT, wallet.integer_gems);
                }
                // 金币大于100w加入排行榜
                if (wallet.integer_gold > GameSettingsInstance.Settings.Leaderboard.GoldLimitMin)
                {
                    await LeaderboardManager.Instance._UpdateLeaderboardRecord(user_data.server_uid, user_data.name, AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT, wallet.integer_gold);
                }
            }

            UserHOLData? hol_data = null;
            this.DBGetHOLData(user_data.server_uid, out hol_data);
            if (hol_data != null)
            {
                if (hol_data.rank_level >= GameSettingsInstance.Settings.Leaderboard.GameRankMinLevel)
                {
                    await LeaderboardManager.Instance._UpdateLeaderboardRecord(user_data.server_uid, user_data.name, hol_data.rank_level, hol_data.rank_value);
                }
            }

            // 获取全部消费记录
            List<UserCashShopItem> cashshop_items = new List<UserCashShopItem>();
            if (await CashShopManager.Instance._GetUserCashItems(user_data.server_uid, cashshop_items) > 0)
            {
                double cost = CashShopManager.Instance.TotalCashItemsCost(cashshop_items);
                //if (cost >= GameSettingsInstance.Settings.Leaderboard.GemsLimitMinWeekly)
                {
                    await LeaderboardManager.Instance._UpdateLeaderboardRecord(user_data.server_uid, user_data.name, AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT, cost);
                }
            }

            return result_code;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task<int> GetUserProfile(string? user_uid, string uid, UserProfileExtend profile)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            ///返回用户不存在
            if (uid.IsNullOrWhiteSpace())
            {
                return 0;
            }

            profile.UID = uid;

            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            // 不是同一个，这里暂时使用同一个
            DBUserProfile? db_profile = null;
            int result_code = this.DBGetUserProfile(user, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.AvatarID = db_profile?.AvatarID ?? 0;
            profile.AvatarUrl = "";
            profile.Gender = db_profile?.Gender ?? (int)UserGender.Female;
            profile.Region = db_profile?.Region ?? "";

            profile.Name = db_profile?.Name ?? "";

            DBUserProfileExtend? db_profile_1 = await this.DBGetUserProfileExtend(user, profile);
            if (db_profile_1 == null)
            {
                return -1;
            }

            profile.LastRankLevel = db_profile_1?.LastRankLevel ?? 1000;
            profile.LastRankValue = db_profile_1?.LastRankValue ?? 0;
            profile.RankLevel = db_profile_1?.RankLevel ?? 1000;
            profile.RankValue = db_profile_1?.RankValue ?? 0;
            profile.Season = db_profile_1?.Season ?? 1;
            profile.SeasonTime = db_profile_1?.SeasonTime ?? null;
            profile.ChallengerReals = db_profile_1?.ChallengerReals ?? 0;
            return 1;
        }

        /// <summary>
        /// 返回-1就是错误，1是成功，0是没有修改成功，可能的原因是重名或道具不足。
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="new_name"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected async Task<int> ChangeUserName(string? user_uid, string to_name, UserProfile profile)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if (to_name.IsNullOrWhiteSpace())
            {
                return 0;
            }
            // 如果返回负数，改用户将不能再修改用户名
            int result_code = 1;
            if ((result_code = AMToolkits.Utils.CheckNameIsValid(to_name)) <= 0)
            {
                // 尝试非法字符
                if (result_code <= -1000)
                {

                }
                return -1;
            }

            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            profile.UID = user_uid;
            // 不是同一个，这里暂时使用同一个
            DBUserProfile? db_profile = null;
            result_code = this.DBGetUserProfile(user, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.InitFromDB(db_profile);

            // 扣除玩家道具
            var list = new List<UserInventoryItem>();
            var value = GameSettingsInstance.Settings.User.NeedChangeNameItems;
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(value);
            if (items != null && !items.IsNullOrEmpty())
            {
                if (await DBGetUserInventoryItems(user.UID, items.ToArray(), list) < 0)
                {
                    return -1;
                }

                // 目前只处理第一个物品
                foreach (var v in items)
                {
                    var ilist = list.Where(vi => vi.index == v.ID).ToList();
                    int count = ilist.Sum(vi => vi.count);
                    if (count < v.Count)
                    {
                        return 0;
                    }
                }
            }

            result_code = await this.DBChangeUserName(user, profile, to_name);
            if (result_code < 0)
            {
                return 0;
            }

            // 扣除玩家道具
            List<NUserInventoryItem> nlist = new List<NUserInventoryItem>();
            if (items != null && items.Length > 0)
            {
                foreach (var v in items)
                {
                    var vlist = new List<NUserInventoryItem>();
                    if (await this.ConsumableUserInventoryItem(user.UID, v.ID, v.Count, vlist, "changed_name") < 0)
                    {
                        _logger?.LogError($"{TAGName} (ChangeUserName) (User:{user_uid}) Change Name to ({to_name}) Failed" +
                                    $", Item {v.ID} ({v.Count}) Error");
                        return 0;
                    }
                    nlist.AddRange(vlist);
                }
            }
            return 1;
        }

    }
}