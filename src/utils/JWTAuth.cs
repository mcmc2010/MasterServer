using System.Security.Cryptography;
using AMToolkits.Extensions;


////
namespace AMToolkits.Utility
{
    /// <summary>
    /// JWT 标准数据类
    /// </summary>
    [System.Serializable]
    public class JWTHeader
    {
        public string alg = "HS256";
        public string typ = "JWT";
    }

    /// <summary>
    /// 
    /// </summary>
    public class JWTAuth
    {
        public static string JWTSignData(string text, string secret_key)
        {
            var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret_key.Trim()));
            var data = System.Text.Encoding.UTF8.GetBytes(text);
            var hash = hmac.ComputeHash(data);
            return hash.Base64UrlEncode();
        }

        public static string JWTSignData(string header, string payload, string secret_key)
        {
            return JWTSignData($"{header.Trim()}.{payload.Trim()}", secret_key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="secret_key"></param>
        /// <param name="expired">默认为24小时</param>
        /// <returns></returns>
        public static string JWTSignData(IDictionary<string, object> payload, string secret_key, int expired = 1*24 * 60 * 60)
        {
            if(secret_key.Length == 0) {
                return "";
            }
            if(expired <= 0) {
                expired = 1*24 * 60 * 60;
            }

            long expired_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expired;
            payload.Add("exp", expired_time);
            var v_1 = System.Text.Json.JsonSerializer.Serialize<IDictionary<string, object>>(payload, 
                new System.Text.Json.JsonSerializerOptions(){
                    IgnoreReadOnlyFields = true,
                    IncludeFields = true, 
                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                    // ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });
            if(string.IsNullOrEmpty(v_1)) {
                return "";
            }

            JWTHeader header = new JWTHeader() {
                alg = "HS256",
                typ = "JWT"
            };
            var v_0 = System.Text.Json.JsonSerializer.Serialize<JWTHeader>(header, 
                new System.Text.Json.JsonSerializerOptions(){
                    IgnoreReadOnlyFields = true,
                    IncludeFields = true, 
                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                    // ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });
            if(string.IsNullOrEmpty(v_0)) {
                return "";
            }

            v_0 = v_0.Base64UrlEncodeFromString();
            v_1 = v_1.Base64UrlEncodeFromString();
            var v_2 = JWTSignData($"{v_0.Trim()}.{v_1.Trim()}", secret_key);
            return $"{v_0}.{v_1}.{v_2}";
        }

        /// <summary>
        /// -1 : 错误, 0 : 认证错误或超时
        /// </summary>
        /// <param name="text"></param>
        /// <param name="secret_key"></param>
        /// <returns></returns>
        public static int JWTVerifyData(string text, string secret_key)
        {
            if(secret_key.Length == 0) {
                return -1;
            }

            var values = text.Split('.');
            if(values.Length < 3) { return -1; }
            try
            {
                string header = values[0];
                string payload = values[1];
                string hash = JWTSignData(header, payload, secret_key);
                if(hash != values[2].Trim())
                {
                    return 0;
                }

                payload = payload.Base64UrlDecode2String();
                var payload_data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payload, 
                    new System.Text.Json.JsonSerializerOptions(){
                        IgnoreReadOnlyFields = true,
                        IncludeFields = true, 
                        // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                        // ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                        // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                    });
                
                object? value;
                if(payload_data == null || !payload_data.TryGetValue("exp", out value))
                {
                    return -1;
                }

                long timestamp = ((System.Text.Json.JsonElement)value).GetInt64();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now > timestamp) {
                    return 0;
                }
                return 1;
            } catch (Exception e) {
                System.Console.WriteLine(e.Message);
                return -1;
            }
        }
    }
}