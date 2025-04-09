
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
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; } = true; // 默认路径
        [YamlMember(Alias = "name")] // 明确映射 YAML 字段名（可选，如果字段名不一致时需要）
        public string Name { get; set; } = ""; // 默认值
        [YamlMember(Alias = "level")] // 明确映射 YAML 字段名（可选，如果字段名不一致时需要）
        public string Level { get; set; } = "Information"; // 默认值

        [YamlMember(Alias = "file")]
        public string File { get; set; } = "logs/main.log"; // 默认路径
        [YamlMember(Alias = "is_console", ApplyNamingConventions = false)]
        public bool IsConsole { get; set; } = true; // 默认开启
        [YamlMember(Alias = "is_file", ApplyNamingConventions = false)]
        public bool IsFile { get; set; } = true; // 默认开启
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

    
    public class ConfigItem_Database
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "main"; 
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = "game"; 
        [YamlMember(Alias = "user")]
        public string UserName { get; set; } = "sa";
        [YamlMember(Alias = "pass")]
        public string Password { get; set; } = "123456";
        [YamlMember(Alias = "address")]
        public string Address { get; set; } = "127.0.0.1"; 
        [YamlMember(Alias = "port")]
        public int Port { get; set; } = 3306; 
        [YamlMember(Alias = "ssl")]
        public bool HasSSL { get; set; } = false;
        [YamlMember(Alias = "ssl_certs", ApplyNamingConventions = false)]
        public string SSLCertificates { get; set; } = "";
        [YamlMember(Alias = "ssl_key", ApplyNamingConventions = false)]
        public string SSLKey { get; set; } = "";
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
        /// JWT 
        /// </summary>
        [YamlMember(Alias = "jwt_enabled", ApplyNamingConventions = false)]
        public bool JWTEnabled { get; set; } = true;
        /// <summary>
        /// JWT
        /// </summary>
        [YamlMember(Alias = "jwt_secret_key", ApplyNamingConventions = false)]
        public string JWTSecretKey { get; set; } = "";
        /// <summary>
        /// JWT Expired
        /// </summary>
        [YamlMember(Alias = "jwt_expired", ApplyNamingConventions = false)]
        public int JWTExpired { get; set; } = 1 * 24 * 60 * 60;
        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "http_server", ApplyNamingConventions = false)]
        public ConfigItem_HTTPServer[] HTTPServer { get; set; } = new ConfigItem_HTTPServer[] { }; // 初始化默认配置
        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "database", ApplyNamingConventions = false)]
        public ConfigItem_Database[] Databases = new ConfigItem_Database[]{ };

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
                    .WithNamingConvention(UnderscoredNamingConvention.Instance) // 自动下划线命名
                    //.WithNamingConvention(CamelCaseNamingConvention.Instance) // 自动驼峰命名
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
