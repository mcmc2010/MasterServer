
namespace AMToolkits.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string? str)
        {
            return str == null || str.Trim().Length == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string Base64UrlEncode(this byte[] bytes)
        {
            string b64 = System.Convert.ToBase64String(bytes);
            return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static string Base64UrlEncodeFromString(this string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            return data.Base64UrlEncode();
        }

        public static byte[] Base64UrlDecode(this string text)
        {
            string b64 = text.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            byte[] bytes = System.Convert.FromBase64String(b64);
            return bytes;
        }

        public static string Base64UrlDecode2String(this string text)
        {
            byte[] data = text.Base64UrlDecode();
            string content = System.Text.Encoding.UTF8.GetString(data);
            return content;
        }
    }
}