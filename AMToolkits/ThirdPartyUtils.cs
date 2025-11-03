
using System.Text.RegularExpressions;


namespace AMToolkits
{
    public class ThirdPartyUtils
    {
        // 支持多种情况的正则表达式
        private static readonly Regex _wx_url_pattern = new Regex(@"
            ^(?:https?://)?                # 可选的协议头
            thirdwx\.qlogo\.cn/mmopen/     # 固定域名和路径
            vi_32/                         # 版本标识
            ([a-zA-Z0-9_-]+)               # 哈希值（分组1）
            /(\d+)                         # 尺寸（分组2）
            (?:/.*)?                       # 可选的子尺寸（如/0, /46等）
            $", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        // 匹配腾讯云COS头像URL的正则表达式
        private static readonly Regex _wxcos_url_pattern = new Regex(@"
            ^(?:https?://)?                # 可选的协议头
            [a-z0-9-]+                     # 子域名部分（如cdn-1251388827）
            \.cos\.[a-z0-9-]+\.myqcloud\.com  # 固定格式的域名
            /mmopen/icons/                 # 固定路径
            ([a-f0-9]{32})                 # 哈希值（分组1）
            _(\d+)                         # 尺寸（分组2）
            (?:[?#].*)?                    # 可选的查询参数或锚点
            $", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static (string? hash, string? size, string? full) WXParseAvatarUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return (null, null, null);
            }

            var match = _wx_url_pattern.Match(url);
            if (!match.Success)
            {
                match = _wxcos_url_pattern.Match(url);
                if (!match.Success)
                {
                    return (null, null, null);
                }
            }

            string hash = match.Groups[1].Value;
            string size = match.Groups[2].Value;
            string full = $"{hash}/{size}";

            return (hash, size, full);
        }
    }
}