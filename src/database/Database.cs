
using AMToolkits.Utility;
using Logger;
using MySql.Data.MySqlClient; // 正确命名空间

namespace Server {

    /// <summary>
    /// 
    /// </summary>
    public class DatabaseManager : SingletonT<DatabaseManager>, ISingleton
    {

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;
        private Logger.LoggerEntry? _logger_query = null;

        public DatabaseManager()
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="paramters"></param>
        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[DatabaseManager] Config is NULL.");
                return ;
            }

            _config = config;

            this.InitLogger();

            this.ProcessWorking();
        }

        private void InitLogger()
        {
            //
            _logger = Logger.LoggerFactory.Instance;
            var cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "database");
            if(cfg != null) {
                if(!cfg.Enabled) {
                    _logger = null;
                }
                else
                {
                    _logger = Logger.LoggerFactory.CreateLogger(cfg.Name, cfg.IsConsole, cfg.IsFile);
                    _logger.SetOutputFileName(cfg.File);
                }
            }

            //
            _logger_query = null;
            cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "database_query");
            if(cfg != null && cfg.Enabled) {
                _logger_query = Logger.LoggerFactory.CreateLogger(cfg.Name, cfg.IsConsole, cfg.IsFile);
                _logger_query.SetOutputFileName(cfg.File);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<int> ProcessWorking()
        {
            this.TestConnection();
            return 0;
        }

        public void TestConnection()
        {
            var db = this.New();
            var result = db?.Query($"SELECT 'Hello World'");
            this.Free(db);
        }

        public DatabaseQuery? New(string type = "main")
        {
           var cfg = _config?.Databases.FirstOrDefault(v => v.Type.Trim().ToLower() == type);
            if(cfg == null) {
                _logger?.LogError($"[DatabaseManager] Config ({type}) not found.");
                return null;
            }

            try
            {
                // 0. 构造连接字符串
                var connection_string = new MySqlConnectionStringBuilder
                {
                    Server = cfg.Address,           // 数据库服务器地址
                    Database = cfg.Name,            // 数据库名称
                    UserID = cfg.UserName,          // 用户名
                    Password = cfg.Password,        // 密码
                    Port = (uint)cfg.Port,          // 默认端口（如修改过需指定）
                    SslMode = cfg.HasSSL ? MySqlSslMode.Required : MySqlSslMode.Disabled,    // 加密模式（根据服务器配置调整）
                    SslCert = cfg.SSLCertificates,     // 客户端证书（PEM格式）
                    SslKey = cfg.SSLKey,       // 客户端私钥（PEM格式）
                }.ToString();

                // 1. 创建连接对象
                var conn = new MySqlConnection(connection_string);

                conn.Open();

                var query = new DatabaseQuery() {
                    name = type,
                    version = conn.ServerVersion,
                    db = conn,
                    logger = _logger_query
                };
                return query;
            }
            catch (Exception e)
            {
                _logger?.LogError($"[DatabaseManager] ({type}) Error : {e.Message} -> {e.InnerException?.Message}");
                return null;
            }
        }

        public void Free(DatabaseQuery? query)
        {
            if(query == null) {
                return;
            }

            try
            {
                if(query.db != null) {
                    query.db.Close();
                    query.db.Dispose();
                    query.db = null;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"[DatabaseManager] ({query?.name}) Error : {e.Message}");
            }
        }
    }
}