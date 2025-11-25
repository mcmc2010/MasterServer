
using System.Text.Json.Serialization;


namespace Server
{
    [System.Serializable]
    public class GameSettings_User
    {
        [JsonPropertyName("user_icons")]
        public string[] UserIcons = new string[] { };

        [JsonPropertyName("using_user_level_experiences_table")]
        public bool UsingUserLevelExperiencesTable = false;
        [JsonPropertyName("user_level_experiences")]
        public long[] UserLevelExperiences = new long[]{ 0, 100 };

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("item_default_equipment")]
        public int ItemDefaultEquipmentIndex = AMToolkits.Game.ItemConstants.ID_NONE;

        /// <summary>
        /// 改名必须使用道具
        /// </summary>
        [JsonPropertyName("need_change_name_items")]
        public string NeedChangeNameItems = "";
        /// <summary>
        /// 单位秒
        /// </summary>
        [JsonPropertyName("need_change_name_time")]
        public int NeedChangeNameTime = 0;
    }

    [System.Serializable]
    public class GameSettings_Season
    {
        [JsonPropertyName("index")]
        public int Index = 1; //赛季序号，s1 = 1

        [JsonPropertyName("code")]
        public int Code = 0; //赛季代码，通常是4位数

        /// <summary>
        /// 通行证
        /// </summary>
        [JsonPropertyName("pass")]
        public Dictionary<int, int[]> PassLevels = new Dictionary<int, int[]>(); 
    }

    [System.Serializable]
    public class GameSettings_Leaderboard
    {
        [JsonPropertyName("gold_limit_min")]
        public int GoldLimitMin = 1000000; //金币最小上榜条件

        [JsonPropertyName("gems_limit_min")]
        public int GemsLimitMin = 5000; // 钻石最小上榜条件

        [JsonPropertyName("gems_limit_min_weekly")]
        public int GemsLimitMinWeekly = 10000; // 钻石最小上榜条件

        [JsonPropertyName("gems_limit_min_monthly")]
        public int GemsLimitMinMonthly = 10000; // 钻石最小上榜条件


        /// <summary>
        /// 上榜最小段位
        /// </summary>
        [JsonPropertyName("game_rank_min_level")]
        public int GameRankMinLevel = 1001;
    }

    [System.Serializable]
    public class GameSettings_Mission
    {
        [JsonPropertyName("daily_enabled")]
        public bool DailyEnabled = true; 

        [JsonPropertyName("daily_event_code")]
        public int DailyGameEventCode = 0; //每日任务关联事件ID
    }

    [System.Serializable]
    public class GameSettings
    {
        [JsonPropertyName("user")]
        public GameSettings_User User = new GameSettings_User();

        [JsonPropertyName("season")]
        public GameSettings_Season Season = new GameSettings_Season();

        [JsonPropertyName("leaderboard")]
        public GameSettings_Leaderboard Leaderboard = new GameSettings_Leaderboard();

        [JsonPropertyName("mission")]
        public GameSettings_Mission Mission = new GameSettings_Mission();
    }

    public class GameSettingsInstance
    {
        private static GameSettings _settings = new GameSettings();
        public static GameSettings Settings { get { return _settings; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static GameSettings? LoadFromFile(string filename)
        {
            try
            {
                var asset = AMToolkits.Utility.ResourcesManager.Load<AMToolkits.Utility.TextAsset>(filename);
                if (asset == null)
                {
                    return _settings;
                }

                // 读取 JSON 文件内容
                string json = asset.text;
                if (json.Length == 0)
                {
                    return _settings;
                }

                var o = System.Text.Json.JsonSerializer.Deserialize<GameSettings>(json,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                }
                );
                if (o == null)
                {
                    return _settings;
                }
                return _settings = o;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loading GameSetting : {ex.Message}");
                return null;
            }
        }
    }
}
