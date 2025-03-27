
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Server {

    public interface IConfigEntry
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public class ConfigItem_Logger
    {
        [YamlMember(Alias = "name")] // 明确映射 YAML 字段名（可选，如果字段名不一致时需要）
        public string Name { get; set; } = ""; // 默认值
        [YamlMember(Alias = "level")] // 明确映射 YAML 字段名（可选，如果字段名不一致时需要）
        public string Level { get; set; } = "Information"; // 默认值

        [YamlMember(Alias = "file")]
        public string File { get; set; } = "logs/main.log"; // 默认路径

        public Logger.LogLevel Getlevel() {
            //
            if(Level == Logger.LogLevel.Debug.ToString())
            {
                return Logger.LogLevel.Debug;
            }
            else if(Level == Logger.LogLevel.Warning.ToString())
            {
                return Logger.LogLevel.Warning;
            }
            else if(Level == Logger.LogLevel.Error.ToString())
            {
                return Logger.LogLevel.Error;
            }
            else
            {
                return Logger.LogLevel.Information;
            }
        }
    }

    public class ConfigItem_HTTPServer
    {
        [YamlMember(Alias = "address")]
        public string Address { get; set; } = "0.0.0.0"; 
        [YamlMember(Alias = "port")]
        public int Port { get; set; } = 5000; 
        [YamlMember(Alias = "ssl")]
        public bool HasSSL { get; set; } = false;
        [YamlMember(Alias = "certs")]
        public string Certificates { get; set; } = "";
    }

    /// <summary>
    /// 
    /// </summary>
    public class ServerConfig : IConfigEntry
    {
        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "secret_key", ApplyNamingConventions = false)]
        public string SecretKey { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "http_server", ApplyNamingConventions = false)]
        public ConfigItem_HTTPServer[] HTTPServer { get; set; } = new ConfigItem_HTTPServer[] { }; // 初始化默认配置

        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "logging", ApplyNamingConventions = false)]
        public ConfigItem_Logger[] Logging { get; set; } = new ConfigItem_Logger[]{
            new ConfigItem_Logger() {
                Name = "main",
                Level = "Information",
                File = "logs/main.log"
            }
        };
    }

    /// <summary>
    /// 
    /// </summary>
    public class ServerConfigLoader
    {
        private static ServerConfig _config = new ServerConfig();
        public static ServerConfig Config { get { return _config; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ServerConfig LoadFromFile(string filename)
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
                var config = deserializer.Deserialize<ServerConfig>(yaml);
                if(config != null)
                {
                    _config = config;
                }

                _config.SecretKey = _config.SecretKey.Trim().ToUpper();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            return _config;
        }
    }
}
