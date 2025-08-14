
using System.Text.Json.Serialization;
using AMToolkits;
using AMToolkits.Extensions;
using Logger;

//
using StackExchange.Redis;




namespace AMToolkits.Redis
{
    [System.Serializable]
    public class RedisOptions
    {
        [JsonPropertyName("name")]
        public string Name = "redis";
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = "127.0.0.1";
        [JsonPropertyName("port")]
        public int Port { get; set; } = 6379;

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("user")]
        public string UserName { get; set; } = "sa";

        [JsonPropertyName("pass")]
        public string Password { get; set; } = "123456";

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("ssl")]
        public bool HasSSL { get; set; } = false;
        [JsonPropertyName("ssl_certs")]
        public string SSLCertificates { get; set; } = "";
        [JsonPropertyName("ssl_key")]
        public string SSLKey { get; set; } = "";
    }

    /// <summary>
    /// 
    /// </summary>
    class RedisLogWriter : TextWriter
    {
        private readonly Logger.LoggerEntry? _logger;

        public RedisLogWriter(Logger.LoggerEntry? logger)
        {
            _logger = logger;
        }

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(char value)
        {
            // 忽略单个字符写入
        }

        public override void Write(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // 根据日志级别分类
                if (value.Contains("ERROR") || value.Contains("Failed"))
                    _logger?.LogError(value);
                else if (value.Contains("WARN") || value.Contains("Warning"))
                    _logger?.LogWarning(value);
                else
                    _logger?.Log(value);
            }
        }

        public override void WriteLine(string? value)
        {
            Write(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RedisManager : AMToolkits.SingletonT<RedisManager>, AMToolkits.ISingleton, System.IDisposable
    {
#pragma warning disable CS0649
        [AutoInitInstance]
        protected static RedisManager? _instance;
#pragma warning restore CS0649

        private string[]? _arguments = null;
        private Logger.LoggerEntry? _logger = null;

        private RedisOptions _options = new RedisOptions()
        {

        };

        private StackExchange.Redis.ConnectionMultiplexer? _connection = null;
        private StackExchange.Redis.IDatabase? _database = null;

        /// <summary>
        /// 当前在使用证书
        /// 仅仅是记录，不做回收
        /// </summary>
        protected static System.Security.Cryptography.X509Certificates.X509Certificate2? _certificate = null;

        /// <summary>
        /// Not call parent method
        /// </summary>
        protected override void OnInitialize(object[] paramters)
        {
            _arguments = CommandLineArgs.FirstParser(paramters);

            var options = paramters[1] as RedisOptions;
            if (options == null)
            {
                System.Console.WriteLine($"{TAGName} RedisOptions is NULL.");
                return;
            }
            _options = options;

            // 初始化日志
            _logger = Logger.LoggerFactory.Instance;
            Logger.LoggerEntry? logger = paramters[2] as Logger.LoggerEntry;
            if (logger != null)
            {
                _logger = logger;
            }

            this.ProcessWorking();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }

            if (_certificate != null)
            {
                _certificate.Dispose();
                _certificate = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private System.Security.Cryptography.X509Certificates.X509Certificate2? LoadCertificate(string cert_filename, string key_filename)
        {
            if (_certificate != null)
            {
                //_certificate.Dispose();
                //_certificate = null;
            }

            // 读取证书
            string cert_pem = "";
            if (cert_filename.Trim().Length > 0)
            {
                cert_pem = File.ReadAllText(cert_filename);
            }

            string key_pem = "";
            if (key_filename.Trim().Length > 0)
            {
                key_pem = File.ReadAllText(key_filename);
            }

            if (string.IsNullOrEmpty(cert_pem) || string.IsNullOrEmpty(key_pem))
            {
                return null;
            }

            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(key_pem);

            // windows 该方法无法导出私钥
            var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(cert_pem, key_pem);
            // 导出为PFX格式
            var p12 = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, (string?)null);
            // 导出可用私钥
            return _certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(p12, (string?)null,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet);
        }



        private async Task<int> ProcessWorking()
        {
            try
            {
                var config = new ConfigurationOptions
                {
                    EndPoints = { $"{_options.Address}:{_options.Port}" }, // 集群节点
                    Password = _options.Password,
                    Ssl = _options.HasSSL,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000,
                    AbortOnConnectFail = false,
                    AllowAdmin = true,
                    ClientName = _options.Name,
                    KeepAlive = 60, // 秒
                    ReconnectRetryPolicy = new ExponentialRetry(5000), // 5秒重试间隔
                    ConnectRetry = 5,
                    DefaultDatabase = 0
                };

                if (config.Ssl && _options.SSLCertificates.Length > 0)
                {
                    // 读取证书
                    this.LoadCertificate(_options.SSLCertificates, _options.SSLKey);
                    if (_certificate == null)
                    {
                        _logger?.LogError($"Redis certificat error.");
                        return -1;
                    }

                    config.SslClientAuthenticationOptions = (host) =>
                    {
                        return new System.Net.Security.SslClientAuthenticationOptions()
                        {
                            TargetHost = host,
                            //
                            ClientCertificates = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection(_certificate),
                            //
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                            {
                                // 自定义证书验证逻辑
                                // 允许自签名证书但拒绝其他错误
                                return errors == System.Net.Security.SslPolicyErrors.None ||
                                    errors == (System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch | System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors);
                            }
                        };
                    };
                }

                _connection = StackExchange.Redis.ConnectionMultiplexer.Connect(config, new RedisLogWriter(_logger));

                // 注册事件处理器
                _connection.ConnectionFailed += (sender, args) =>
                    _logger?.LogError($"Redis connection failed: {args.Exception}");

                _connection.ConnectionRestored += (sender, args) =>
                    _logger?.Log("Redis connection restored");

                _connection.ErrorMessage += (sender, args) =>
                    _logger?.LogError($"Redis error: {args.Message}");

                _connection.InternalError += (sender, args) =>
                    _logger?.LogError($"Redis internal error: {args.Exception}");

                _logger?.Log("Redis connection start");

                _database = this.GetDatabase();

                //
                this.Ping();
                //
                this.Performances();
            }
            catch (RedisConnectionException e)
            {
                _logger?.LogException($"Failed to connect to Redis : {e.Message}", e);
                return -1;
            }
            return 0;
        }

        public StackExchange.Redis.IServer? GetServer(string? endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }
            return _connection?.GetServer(endpoint);
        }

        public StackExchange.Redis.IDatabase? GetDatabase(int db = -1)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                return null;
            }

            return _connection.GetDatabase(db);
        }

        public bool Ping()
        {
            try
            {
                long tick = AMToolkits.Utils.GetLongTimestamp();
                _database?.Ping();
                float ms = AMToolkits.Utils.DiffTimestamp(tick, AMToolkits.Utils.GetLongTimestamp());
                _logger?.Log($"Redis checking ok [{ms:F2} s]");
                return true;
            }
            catch (Exception e)
            {
                _logger?.LogError($"Redis checking: {e.Message}", e);
                return false;
            }
        }

        public void Performances()
        {
            var counters = _connection?.GetCounters();
            _logger?.Log($"{TAGName} Performances: " +
                $"Connections Count: {counters?.Interactive.SocketCount}, Operation Count: {counters?.Interactive.OperationCount}, ");
        }

        private T?  ToJsonDeserialize<T>(string json)
        {
            try
            {
                //
                var o = System.Text.Json.JsonSerializer.Deserialize<T>(json,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                IgnoreReadOnlyFields = true,
                                IgnoreReadOnlyProperties = true,
                                IncludeFields = true,  
                                // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                            }
                );
                return o;
            }
            catch (Exception e)
            {
                _logger?.LogException($"{e.Message}", e);
                return default(T?);
            }
        }

        private string ToJsonSerializer(object? o)
        {
            string json = "";
            try
            {
                if (o != null)
                {
                    json = System.Text.Json.JsonSerializer.Serialize(o,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            IgnoreReadOnlyFields = true,
                            IgnoreReadOnlyProperties = true,
                            IncludeFields = true,
                            // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                                                                              // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                        });
                }
                return json;
            }
            catch (Exception e)
            {
                _logger?.LogException($"{e.Message}", e);
                return json;
            }
        }

        public void SetNodeKey(string node)
        {
            if (_database == null)
            {
                return;
            }

            // 已经存在节点就不处理
            if (_database.KeyExists(node))
            {
                return;
            }

            _database.HashSet(node, new HashEntry[] {
                    new HashEntry("-1", "")
                },
                CommandFlags.None);
        }

        /// <summary>
        /// 设置或增加key，value
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void SetNodeKeyValue(string node, string key, object? val)
        {
            SetNodeKeyValue(node, key, this.ToJsonSerializer(val));
        }

        public void SetNodeKeyValue(string node, string key, string? val)
        {
            if (_database == null || key.IsNullOrWhiteSpace())
            {
                return;
            }

            _database.HashSet(node, key, val ?? RedisValue.Null, When.Always, CommandFlags.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="expired"></param>
        public void SetKeyValue(string node, string key, object? val, int expired = -1)
        {
            SetKeyValue(node, key, this.ToJsonSerializer(val), expired);
        }

        public void SetKeyValue(string key, string? val, int expired = -1)
        {
            if (_database == null || key.IsNullOrWhiteSpace())
            {
                return;
            }

            _database.StringSet(key, val ?? RedisValue.Null,
                expired < 0 ? null : TimeSpan.FromSeconds(expired), false, When.Always, CommandFlags.None);
        }

        /// <summary>
        /// 获取Key, Value
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? GetNodeKeyValueT<T>(string node, string key)
        {
            string? json = GetNodeKeyValue(node, key);
            if (json == null)
            {
                return default(T?);
            }
            return this.ToJsonDeserialize<T>(json);
        }

        public string? GetNodeKeyValue(string node, string key)
        {
            if (_database == null || key.IsNullOrWhiteSpace())
            {
                return null;
            }

            var value = _database.HashGet(node, key, CommandFlags.None);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return value.ToString();
        }

        public T? GetKeyValueT<T>(string node, string key)
        {
            string? json = GetKeyValue(node, key);
            if (json == null)
            {
                return default(T?);
            }
            return this.ToJsonDeserialize<T>(json);
        }

        public string? GetKeyValue(string key, string? val, int expired = -1)
        {
            if (_database == null || key.IsNullOrWhiteSpace())
            {
                return null;
            }

            var value = _database.StringGet(key, CommandFlags.None);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return value.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        public void DeleteNodeKeyValue(string node, string key)
        {
            if (_database == null || key.IsNullOrWhiteSpace())
            {
                return;
            }
            if (!_database.HashDelete(node, key, CommandFlags.None))
            {
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task FlushDatabaseAsync(int db = -1)
        {
            if (_connection == null)
            {
                return;
            }

            //
            var endpoints = _connection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = GetServer(endpoint?.ToString());
                if (server != null)
                {
                    await server.FlushDatabaseAsync(db);
                }
            }
        }

    }
}