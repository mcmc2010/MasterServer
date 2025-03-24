using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Server {

    /// <summary>
    /// 
    /// </summary>
    public class ConfigLogging
    {
        [YamlMember(Alias = "level")] // 明确映射 YAML 字段名（可选，如果字段名不一致时需要）
        public string Level { get; set; } = "Information"; // 默认值

        [YamlMember(Alias = "file")]
        public string File { get; set; } = "logs/main.log"; // 默认路径
    }

    /// <summary>
    /// 
    /// </summary>
    public class ServerConfig
    {
        [YamlMember(Alias = "logging")]
        public ConfigLogging Logging { get; set; } = new ConfigLogging(); // 初始化默认配置
    }

    /// <summary>
    /// 
    /// </summary>
    public class ServerConfig
    {
        private static Config _config = new Config();
        public static Config LoadConfigFromFile(string filename)
        {
            try
            {
                // 读取 YAML 文件内容
                string yaml = File.ReadAllText(filename);

                // 创建反序列化器
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance) // 自动将驼峰命名映射到 PascalCase 属性
                    .Build();

                // 反序列化为对象
                var config = deserializer.Deserialize<Config>(yaml);
                if(config != null)
                {
                    _config = config;
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            return _config;
        }
    }
}