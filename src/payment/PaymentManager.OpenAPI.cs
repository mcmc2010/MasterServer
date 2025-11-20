
using System.Security.Cryptography.X509Certificates;


using AMToolkits.Extensions;
using AMToolkits.Net;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        #region Wechat
        public async Task<T?> WechatOpenAPIGet<T>(string endpoint,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null)
                        where T : HTTPResponseResult
        {
            if (_wx_client_factory == null)
            {
                return default(T);
            }


            Dictionary<string, object> additional_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (var v in headers)
                {
                    additional_headers[v.Key] = v.Value;
                }
            }

            return await _wx_client_factory.GetAsync<T>(endpoint, arguments, additional_headers);
        }

        public async Task<T?> WechatOpenAPIPost<T>(string endpoint,
                        Dictionary<string, object?>? payload,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null)
                        where T : HTTPResponseResult
        {
            if (_wx_client_factory == null)
            {
                return default(T);
            }

            Dictionary<string, object> additional_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            additional_headers.Add("accept", "application/json");
            if (headers != null)
            {
                foreach (var v in headers)
                {
                    additional_headers[v.Key] = v.Value;
                }
            }

            if(payload == null)
            {
                payload = new Dictionary<string, object?>();
            }
            if (_settings.Wechat.IsSandbox)
            {
                payload.Set("appid", _settings.Wechat.SandBoxAppID);
            }
            else
            {
                payload.Set("appid", _settings.Wechat.AppID);
            }
            

            return await _wx_client_factory.PostAsync<T>(endpoint, payload, additional_headers, arguments);
        }
        #endregion

        #region Alipay
        /// <summary>
        /// https://openapi-sandbox.dl.alipaydev.com/gateway.do?method=alipay.trade.query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="arguments"></param>
        /// <param name="headers"></param>
        /// <param name="is_authentication"></param>
        /// <returns></returns>
        public async Task<T?> AlipayOpenAPIGet<T>(string endpoint,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null)
                        where T : HTTPResponseResult
        {
            if (_al_client_factory == null)
            {
                return default(T);
            }


            Dictionary<string, object> additional_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (var v in headers)
                {
                    additional_headers[v.Key] = v.Value;
                }
            }


            if (_settings.Alipay.IsSandbox)
            {
                arguments?.Set("app_id", _settings.Alipay.SandBoxAppID);
            }
            else
            {
                arguments?.Set("app_id", _settings.Alipay.AppID);
            }
            arguments?.Set("charset", "utf-8");
            arguments?.Set("format", "JSON");
            arguments?.Set("method", _settings.Alipay.Method);
            arguments?.Set("sign_type", "RSA2");

            string timestamp = AMToolkits.Utils.DateTimeToString(null, "yyyy-MM-dd HH:mm:ss");
            arguments?.Set("timestamp", timestamp);

            arguments = arguments?.OrderBy(v => v.Key).ToDictionary();

            string payload = AMToolkits.Net.HTTPClientProxy.ParseArguments("", arguments);

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(payload);
            byte[]? sign_data = null;
            if (!AMToolkits.RSA.RSA2SignData(buffer, _certificate?.GetRSAPrivateKey(), out sign_data))
            {
            }
            else if (sign_data != null)
            {
                string b64 = sign_data.Base64Encode();
                b64 = System.Uri.EscapeDataString(b64);
                arguments?.Set("sign", b64);
            }

            timestamp = System.Uri.EscapeDataString(timestamp);
            arguments?.Set("timestamp", timestamp);

            string? biz_content = arguments?.Get("biz_content") as string;
            if (biz_content != null)
            {
                biz_content = System.Uri.EscapeDataString(biz_content);
                arguments?.Set("biz_content", biz_content);
            }
            
            return await _al_client_factory.GetAsync<T>(endpoint, arguments, additional_headers);
        }
        #endregion

    }
}

