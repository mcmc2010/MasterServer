

using AMToolkits.Extensions;
using Logger;

namespace Server
{
    public class ServiceData
    {
        public string EndPoint = "";
        public bool IsInternalService = false;
        public List<string>? AllowAddressList = null;
    }

    public partial class ServerApplication
    {
        /// <summary>
        /// 当前在使用证书
        /// 仅仅是记录，不做回收
        /// </summary>
        protected static Dictionary<string, System.Security.Cryptography.X509Certificates.X509Certificate2?> _certificates =
                                                new Dictionary<string, System.Security.Cryptography.X509Certificates.X509Certificate2?>(StringComparer.OrdinalIgnoreCase);

        protected static Dictionary<System.Net.IPEndPoint, ServiceData> _webservices = new Dictionary<System.Net.IPEndPoint, ServiceData>();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private System.Security.Cryptography.X509Certificates.X509Certificate2? LoadCertificate(string cert_filename, string key_filename)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(cert_filename) ?? "cert";
            System.Security.Cryptography.X509Certificates.X509Certificate2? cert;
            if (_certificates.TryGetValue(name, out cert) && cert != null)
            {
                //cert.Dispose();
                //cert = null;
                return cert;
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
            cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(cert_pem, key_pem);
            // 导出为PFX格式
            var p12 = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, (string?)null);
            // 导出可用私钥
            cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(p12, (string?)null,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet);

            _certificates.Add(name, cert);

            _logger?.Log($"{TAGName} Load Certificate ({name}) : {cert.GetSerialNumberString()} ");
            return cert;
        }

        /// <summary>
        /// 检测EndPoint 是否访问内部服务
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public ServiceData? CheckInternalService(System.Net.IPAddress? address, int port)
        {
            if (address == null)
            {
                address = System.Net.IPAddress.Any;    
            }

            // 只判断本地端口是否为内部服务
            var service = _webservices.FirstOrDefault(v => v.Key.Port == port);
            return service.Value;
        }
    }
}