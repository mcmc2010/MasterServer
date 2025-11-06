
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
        /// <summary>
        /// 是否为新用户
        /// </summary>
        public bool is_new_user = false;
        /// <summary>
        /// 是否为测试用户
        /// </summary>
        public bool is_test_user = false;

        ///
        public string custom_id = "";
        ///
        public string jwt_token = "";


        //Link Account:
        public string? link_name;
        public string? link_id;
        public string? link_token;
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
        /// 
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl = "";

        public void InitFromDB(DBUserProfile? profile)
        {
            this.AvatarUrl = profile?.AvatarUrl ?? "";
            this.Gender = profile?.Gender ?? (int)UserGender.Female;
            this.Region = profile?.Region ?? "";

            this.Name = profile?.Name ?? "";
        }
    }

    [System.Serializable]
    public class UserProfileExtend : UserProfile
    {
        /// <summary>
        /// 用户等级（不是账号等级）
        /// </summary>
        [JsonPropertyName("level")]
        public int Level = 0;

        [JsonPropertyName("cp_value")]
        public int CPValue = 0;
        
        /// <summary>
        /// 用户经验
        /// </summary>
        [JsonPropertyName("experience")]
        public long Experience = 0;
        [JsonPropertyName("experience_max")]
        public long ExperienceMax = 0;

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
        /// 当前赛季 段位 最好
        /// </summary>
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
        /// 玩家游戏局数
        /// </summary>
        [JsonPropertyName("played_count")]
        public int PlayedCount = 0;

        /// <summary>
        /// 玩家获胜游戏局数
        /// </summary>
        [JsonPropertyName("played_win_count")]
        public int PlayedWinCount = 0;
        
        /// <summary>
        /// 玩家游戏局数
        /// </summary>
        [JsonPropertyName("season_played_count")]
        public int SeasonPlayedCount = 0;

        /// <summary>
        /// 玩家获胜游戏局数
        /// </summary>
        [JsonPropertyName("season_played_win_count")]
        public int SeasonPlayedWinCount = 0;
    }
    #endregion


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async Task<int> _GetUserProfile(string user_uid, UserProfileExtend profile)
        {
            if (user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            ///返回用户不存在
            profile.UID = user_uid;

            // 不是同一个，这里暂时使用同一个
            DBUserProfile? db_profile = null;
            int result_code = this.DBGetUserProfile(profile.UID, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.AvatarUrl = db_profile?.AvatarUrl ?? "";
            profile.Gender = db_profile?.Gender ?? (int)UserGender.Female;
            profile.Region = db_profile?.Region ?? "";

            profile.Name = db_profile?.Name ?? "";

            DBUserProfileExtend? db_profile_1 = await this.DBGetUserProfileExtend(profile.UID, profile);
            if (db_profile_1 == null)
            {
                return -1;
            }

            //
            _InitUserLevelAndExperiences(db_profile_1, profile);

            //
            profile.CPValue = db_profile_1.CPValue;
            profile.LastRankLevel = db_profile_1?.LastRankLevel ?? 1000;
            profile.LastRankValue = db_profile_1?.LastRankValue ?? 0;
            profile.RankLevel = db_profile_1?.RankLevel ?? 1000;
            profile.RankValue = db_profile_1?.RankValue ?? 0;
            profile.RankLevelBest = db_profile_1?.RankLevelBest ?? 1000;
            profile.Season = db_profile_1?.Season ?? 1;
            profile.SeasonTime = db_profile_1?.SeasonTime ?? null;
            profile.ChallengerReals = db_profile_1?.ChallengerReals ?? 0;
            profile.PlayedCount = db_profile_1?.PlayedCount ?? 0;
            profile.PlayedWinCount = db_profile_1?.PlayedWinCount ?? 0;
            profile.SeasonPlayedCount = db_profile_1?.SeasonPlayedCount ?? 0;
            profile.SeasonPlayedWinCount = db_profile_1?.SeasonPlayedWinCount ?? 0;
            return 1;
        }

        #endregion

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

            // 4 - 0:
#if USING_REDIS
            if(!AMToolkits.Redis.RedisManager.Instance.IsInitialized)
            {
                _logger?.LogError($"(User) Auth User (ClientUID:{user_data.client_uid} - {user_data.server_uid}) Failed, " 
                        + "Redis Not Initialize ");
                return -1;
            }
#endif
            // 4 - 1:
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

            // 5: 
            if ((result_code = await this._InitUserData(user_data)) < 0)
            {
                return result_code;
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
                if (cost >= GameSettingsInstance.Settings.Leaderboard.GemsLimitMinWeekly)
                {
                    await LeaderboardManager.Instance._UpdateLeaderboardRecord(user_data.server_uid, user_data.name, AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT, cost);
                }
            }

            return result_code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_data"></param>
        /// <returns></returns>
        protected async Task<int> _InitUserData(UserAuthenticationData user_data)
        {
            // 0: 玩家物品
            var items = await PlayFabService.Instance.PFGetInventoryItems(user_data.server_uid, user_data.custom_id, "user_init");
            if (items != null && items.Data?.ItemList != null)
            {
                int result_count = 0;
                if ((result_count = await this._DBUpdateUserInventoryItems(user_data.server_uid, [.. items.Data.ItemList])) < 0)
                {
                    _logger?.LogWarning($"(User:{user_data.server_uid}) UpdateInventoryItems Failed (Count: {items.Data.ItemList.Length}) Result:{result_count}");
                    return 0;
                }
            }
            else
            {
                _logger?.LogWarning($"(User:{user_data.server_uid}) GetInventoryItems Failed");
                return 0;
            }

            //
            List<UserInventoryItem> inventory_items = new List<UserInventoryItem>();
            if (await this.DBGetUserInventoryItems(user_data.server_uid, inventory_items, (int)AMToolkits.Game.ItemType.Equipment) < 0)
            {
                _logger?.LogWarning($"(User:{user_data.server_uid}) GetInventoryItems Failed");
                return 0;
            }

            var default_equipment = inventory_items.FirstOrDefault(v => v.index == GameSettingsInstance.Settings.User.ItemDefaultEquipmentIndex ||
                    v.expired_time == null);
            if (default_equipment == null)
            {
                List<AMToolkits.Game.GeneralItemData> kit_items = new List<AMToolkits.Game.GeneralItemData>();
                await _GrantBeginnerKitItems(user_data.server_uid, "1000", kit_items);
            }
            return 1;
        }

        protected void _InitUserLevelAndExperiences(DBUserProfileExtend db_profile, UserProfileExtend profile)
        {
            profile.Level = db_profile.Level;
            profile.Experience = db_profile.Experience;
                
            bool using_table = GameSettingsInstance.Settings.User.UsingUserLevelExperiencesTable;
            if (!using_table)
            {

                int level_max = GameSettingsInstance.Settings.User.UserLevelExperiences.Length - 1;
                profile.ExperienceMax = GameSettingsInstance.Settings.User.UserLevelExperiences[level_max];
                if (profile.Level + 1 < level_max)
                {
                    profile.ExperienceMax = GameSettingsInstance.Settings.User.UserLevelExperiences[profile.Level + 1];
                }

            }
            else
            {
                var templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TPlayerLevel>();
                var levels = templates_data?.ToList();
                if(levels != null && levels.Count > 0)
                {
                    levels = levels.OrderBy(v => v.Level).ToList();
                    
                    var level_max = levels[levels.Count - 1];
                    profile.ExperienceMax = level_max.Exp;
                    if(profile.Level + 1 <= level_max.Level)
                    {
                        profile.ExperienceMax = levels.FirstOrDefault(v => v.Level == profile.Level + 1)?.Exp ?? 0;
                    }
                }

            
            }
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
            int result_code = this.DBGetUserProfile(user.ID, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.AvatarUrl = db_profile?.AvatarUrl ?? "";
            profile.Gender = db_profile?.Gender ?? (int)UserGender.Female;
            profile.Region = db_profile?.Region ?? "";

            profile.Name = db_profile?.Name ?? "";

            DBUserProfileExtend? db_profile_1 = await this.DBGetUserProfileExtend(user.ID, profile);
            if (db_profile_1 == null)
            {
                return -1;
            }

            //
            _InitUserLevelAndExperiences(db_profile_1, profile);

            //
            profile.CPValue = db_profile_1.CPValue;
            profile.LastRankLevel = db_profile_1?.LastRankLevel ?? 1000;
            profile.LastRankValue = db_profile_1?.LastRankValue ?? 0;
            profile.RankLevel = db_profile_1?.RankLevel ?? 1000;
            profile.RankValue = db_profile_1?.RankValue ?? 0;
            profile.RankLevelBest = db_profile_1?.RankLevelBest ?? 1000;
            profile.Season = db_profile_1?.Season ?? 1;
            profile.SeasonTime = db_profile_1?.SeasonTime ?? null;
            profile.ChallengerReals = db_profile_1?.ChallengerReals ?? 0;
            profile.PlayedCount = db_profile_1?.PlayedCount ?? 0;
            profile.PlayedWinCount = db_profile_1?.PlayedWinCount ?? 0;
            profile.SeasonPlayedCount = db_profile_1?.SeasonPlayedCount ?? 0;
            profile.SeasonPlayedWinCount = db_profile_1?.SeasonPlayedWinCount ?? 0;
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="to_name"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        protected async Task<int> UpdateUserProfile(string? user_uid, UserProfile profile)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            profile.UID = user_uid;

            // 是否在有效头像范围
            profile.AvatarUrl = profile.AvatarUrl.Trim().ToLower();
            if (!profile.AvatarUrl.IsNullOrWhiteSpace())
            {
                if (profile.AvatarUrl.StartsWith("icon:"))
                {
                    if (!GameSettingsInstance.Settings.User.UserIcons.Any(v => v == profile.AvatarUrl))
                    {
                        return -1;
                    }
                }

                // 微信头像地址
                // https://thirdwx.qlogo.cn/mmopen/vi_32/CQhjoje02HHDAOC50Y55FWb1pdmJCcrkU1LcicraasUtlloODmU1N3cBWN7Gic82NONM7u4yWlFeIibDEibNCtoBzQ/132
                else if (profile.AvatarUrl.StartsWith("https:") && profile.AvatarUrl.Contains("mmopen"))
                {
                    var (hash, size, _) = AMToolkits.ThirdPartyUtils.WXParseAvatarUrl(profile.AvatarUrl);
                    if (hash != null)
                    {
                        profile.AvatarUrl = $"wechat://{hash}:{size}";
                    }
                    else
                    {
                        profile.AvatarUrl = "";
                    }
                }
                else
                {
                    profile.AvatarUrl = "";
                }
            }

            // 不是同一个，这里暂时使用同一个
            DBUserProfile? db_profile = null;
            var result_code = this.DBGetUserProfile(user.ID, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.Name = (profile.Name ?? "").Trim();
            // 当昵称为null时才更新
            if(!string.IsNullOrWhiteSpace(db_profile?.Name))
            {
                //profile.Name = "";
            }
            if(string.IsNullOrWhiteSpace(db_profile?.AvatarUrl))
            {
            }

            result_code = await this.DBUpdateUserProfile(user, db_profile, profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.InitFromDB(db_profile);
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
            result_code = this.DBGetUserProfile(user.ID, profile.UID, out db_profile);
            if (result_code < 0)
            {
                return -1;
            }

            profile.InitFromDB(db_profile);

            // 同一个名字无需再修改
            if (profile.Name == to_name)
            {
                return 0;
            }

            // 检测时间是否限制
            if (db_profile?.ChangedTime != null && GameSettingsInstance.Settings.User.NeedChangeNameTime > 0)
            {
                var changed_time = db_profile.ChangedTime?.AddSeconds(GameSettingsInstance.Settings.User.NeedChangeNameTime);
                var timespan = changed_time - DateTime.Now;
                if (timespan?.TotalSeconds > 0)
                {
                    return -101;
                }
            }


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

                    // 扣除物品失败是否要回退?
                    if (await this.ConsumableUserInventoryItem(user.UID, v.ID, v.Count, vlist, "changed_name") < 0)
                    {
                        _logger?.LogError($"{TAGName} (ChangeUserName) (User:{user_uid}) Change Name to ({to_name})," +
                                    $", ConsumableItem {v.ID}[{v.Count}], Failed");
                        return 0;
                    }
                    nlist.AddRange(vlist);
                }
            }
            return 1;
        }

    }
}