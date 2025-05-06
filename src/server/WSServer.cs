

using AMToolkits.Utility;
using WebSocketSharp.Server;

namespace Server
{
    public partial class WSSServer 
    {
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private WebSocketServer? _server;

        public WSSServer()
        {

        }

        public void Destory()
        {
            if(_server != null)
            {
                _server.Stop();
                _server = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paramters"></param>
        public int Create(params object?[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[WSSServer] Config is NULL.");
                return -1;
            }

            _config = config;

            this.InitLogger();

            _server = new WebSocketServer(_config?.WSServer.Port ?? 5900, _config?.WSServer.HasSSL == true);
            _server.Log.Level = WebSocketSharp.LogLevel.Info;
            if(_logger?.File != null)
            {
                _server.Log.File = System.IO.Path.Join(((Logger.FileLogger)_logger.File).GetPathName(), "ws.log");
            }

            
            if(_config?.WSServer.HasSSL == true && _config?.WSServer.SSLCertificates.Length > 0)
            {
                // 读取证书
                string cert_pem = File.ReadAllText(_config.WSServer.SSLCertificates);
                string key_pem = "";
                if(_config.WSServer.SSLKey.Length > 0) {
                    key_pem = File.ReadAllText(_config.WSServer.SSLKey);
                }

                var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(cert_pem, key_pem);
                var pfx = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, "");
                _server.SslConfiguration.ServerCertificate = 
                    new System.Security.Cryptography.X509Certificates.X509Certificate2(pfx, (string?)null);
                
                // 设置SSL
                //_server.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                _server.SslConfiguration.ClientCertificateRequired = false;
                _server.SslConfiguration.ClientCertificateValidationCallback = 
                (object sender,  
                    System.Security.Cryptography.X509Certificates.X509Certificate? certificate, 
                    System.Security.Cryptography.X509Certificates.X509Chain? chain, 
                    System.Net.Security.SslPolicyErrors errors) => {
                    // 客户端必须提供有效证书
                    // if(certificate == null)
                    // {
                    //     return false;
                    // }
                    // 允许自签名证书但拒绝其他错误
                    return errors == System.Net.Security.SslPolicyErrors.None ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable;
                };
            }

            //_server.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.Anonymous;
            //_server.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.None;

            //
            _server.AddWebSocketService<Server.Packets.Echo>("/");
            _server.AddWebSocketService<Server.Packets.Echo>("/echo");
            _server.AddWebSocketService<Server.Packets.Chat>("/chat");

            //
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitLogger()
        {
            //
            _logger = null;
            var cfg = _config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "server");
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
            if(_logger == null) {
                _logger = Logger.LoggerFactory.Instance;
            }
        }

        public Task<int> StartWorking()
        {

            return this.ProcessWorking();
        }

        private async Task<int> ProcessWorking()
        {
            //
            _server?.Start();


            return 0;
        }
    }
}