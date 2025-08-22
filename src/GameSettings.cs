
using System.Text.Json.Serialization;


namespace Server
{
    [System.Serializable]
    public class GameSettings_Ranking
    {
        [JsonPropertyName("gold_limit_min")]
        public int GoldLimitMin = 1000000; //金币最小上榜条件

        [JsonPropertyName("gems_limit_min")]
        public int GemsLimitMin = 5000; // 钻石最小上榜条件

        [JsonPropertyName("gems_limit_min_weekly")]
        public int GemsLimitMinWeekly = 10000; // 钻石最小上榜条件

        [JsonPropertyName("gems_limit_min_monthly")]
        public int GemsLimitMinMonthly = 10000; // 钻石最小上榜条件
    }

    [System.Serializable]
    public class GameSettings
    {
        [JsonPropertyName("ranking")]
        public GameSettings_Ranking Ranking = new GameSettings_Ranking();
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
        public static GameSettings LoadFromFile(string filename)
        {
            try
            {
                // 读取 JSON 文件内容
                string json = "";
                if (File.Exists(filename))
                {
                    json = File.ReadAllText(filename, System.Text.Encoding.UTF8);
                }
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
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            return _settings;
        }
    }
}