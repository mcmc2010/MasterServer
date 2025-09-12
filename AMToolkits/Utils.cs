#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
#define USE_UNITY_BUILD
#endif

#pragma warning disable CS0168

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AMToolkits.Extensions;

#if USE_UNITY_BUILD
using UnityEngine;
#endif

namespace AMToolkits
{
    /// <summary>
    /// 
    /// </summary>
    public static class Utils
    {
        #region  DateTime
        /// <summary>
        /// 不包含时区
        /// </summary>
        public const string DATETIME_FORMAT_STRING = "yyyyMMdd-HH:mm:ss";
        /// <summary>
        /// 包含时区
        /// </summary>
        public const string DATETIME_FORMAT_LONG_STRING = "yyyyMMdd-HH:mm:ss zz";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetTimestamp()
        {
            DateTime tn = DateTime.UtcNow;
            DateTime t0 = new DateTime(1970, 1, 1);
            TimeSpan span = tn - t0;
            return (int)span.TotalSeconds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static long GetLongTimestamp()
        {
            DateTime tn = DateTime.UtcNow;
            DateTime t0 = new DateTime(1970, 1, 1);
            TimeSpan span = tn - t0;
            return (long)span.TotalMilliseconds;
        }

        /// <summary>
        /// 单位秒
        /// </summary>
        /// <returns></returns>
        public static float DiffTimestamp(int A, int B)
        {
            return (float)(B - A);
        }

        /// <summary>
        /// 单位秒
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static float DiffTimestamp(long A, long B)
        {
            return (float)(B - A) * 0.001f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string DateTimeToString(DateTime? dt = null, string format = DATETIME_FORMAT_STRING)
        {
            try
            {
                if (dt == null)
                {
                    dt = DateTime.Now;
                }
                return ((DateTime)dt).ToString(format);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="format"></param>
        /// <returns>默认赋值了，这里将强制转换为不为NULL</returns>
        public static DateTime DateTimeFromString(string date, string format = DATETIME_FORMAT_STRING, DateTime? defval = null)
        {
            if (defval == null)
            {
                defval = DateTime.Now;
            }

            try
            {
                date = date.Trim();
                if (date == null || date.Length == 0)
                {
                    return (DateTime)defval;
                }

                DateTime dt = DateTime.Now;
                if (!DateTime.TryParseExact(date, format, null, System.Globalization.DateTimeStyles.None, out dt))
                {
                    return (DateTime)defval;
                }

                return dt;
            }
            catch (Exception e)
            {
                return (DateTime)defval;
            }
        }
        #endregion

        #region String
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[]? bytes)
        {
            string text = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    text += string.Format("{0:X2}", bytes[i]);
                }
            }
            return text;
        }

        public static byte[]? ToHexBinary(this string text)
        {
            byte[]? bytes = null;

            text = text.Trim().Replace(":", "").Replace(",", "").Replace(" ", "");
            // Validate even length after cleaning
            if (text.Length % 2 != 0)
            {
                return null;
            }

            // Handle empty string after processing
            if (text.Length == 0)
            {
                return null;
            }

            int bytes_length = text.Length / 2;
            if (bytes_length > 0)
            {
                bytes = new byte[bytes_length];
                for (int i = 0; i < bytes_length; i++)
                {
                    string hex = text.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(hex, 16);  // Correct hex parsing
                }
            }
            return bytes;
        }

        #endregion

        #region IDs

#if !USE_UNITY_BUILD
        /// <summary>
        /// SHA256
        /// </summary>
        /// <returns></returns>
        public static string GetDeviceUniqueIdentifier()
        {
            string uid = $"{System.Environment.MachineName}_" +
                         $"{System.Environment.UserName}_" +
                         $"{System.Environment.OSVersion.VersionString}";
            uid = uid + "_" + System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "_";
            // 增加网卡唯一识别
            string mac = NetworkStatus.GetMacAddress();
            if (mac.Length > 0)
            {
                uid = uid + "_" + mac;
            }
            var sha = System.Security.Cryptography.SHA256.Create();
            // 字符串转 byte 数组
            byte[] hash = System.Text.Encoding.UTF8.GetBytes(uid);
            hash = sha.ComputeHash(hash);
            uid = hash.ToHexString();
            return uid;
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string MakeUserUID(string key = "")
        {
#if USE_UNITY_BUILD
            string uid = UnityEngine.SystemInfo.deviceUniqueIdentifier;
#else
            string uid = GetDeviceUniqueIdentifier();
#endif
            if (key.Trim().Length > 0)
            {
                uid = key.Trim();
            }

            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            // 字符串转 byte 数组
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(uid);
            // 计算 MD5 值
            byte[] digest = md5.ComputeHash(buffer);

            uid = digest.ToHexString();
            return uid;
        }

        #endregion

        #region Name

        /// <summary>
        /// 
        /// </summary>
        // 预留
        private static readonly HashSet<string> ReservedNames = new HashSet<string>
        {
            "admin", "administrator", "moderator", "system", "support",
            "null", "undefined", "test", "guest", "anonymous", "root", "sa",
            "linux", "windows", "macos", "unix", "android", "iphone"
        };
        // 基础敏感词列表（实际应用中应从数据库或文件加载）
        private static readonly HashSet<string> BannedWords = new HashSet<string>
        {
            "傻逼", "草泥马", "fuck", "shit", "asshole", "bitch",
            "妈的", "狗日的", "混蛋", "王八蛋", "操", "日",
            "nmsl", "cnm", "tmd", "sb", "nm", "tm",

        };
        // 政治类敏感词
        private static readonly HashSet<string> SensitiveWords = new HashSet<string>()
        {
            "人民", "人民币", "中国", "china",
            "共产党", "国际", "国家", "中华", "民族"
        };

        public static bool IsReservedName(string name)
        {
            return ReservedNames.Contains(name.ToLowerInvariant());
        }

        public static bool IsBannedWords(string name)
        {
            return BannedWords.Contains(name.ToLowerInvariant()) || SensitiveWords.Contains(name.ToLowerInvariant());
        }

        public static int CheckNameIsValid(string name, int min = 6, int max = 16)
        {
            if (name.IsNullOrWhiteSpace() || name.Any(char.IsWhiteSpace))
            {
                return 0;
            }

            if (name.Length >= max || name.Length < min)
            {
                return 0;
            }

            if (IsReservedName(name))
            {
                return -1000;
            }
            if (IsBannedWords(name))
            {
                return -1001;
            }

            // 定义不允许的标点符号
            string invalid_chars = ",.[]<>?/|~`!@#$%^&*(){}<>_+-=";
            // 检查是否包含不允许的字符
            if (name.Any(c => invalid_chars.Contains(c)))
            {
                return 0;
            }

            // 检查是否包含汉字
            bool has_encode = name.Any(c =>
                {
                    return (c >= 0x4E00 && c <= 0x9FFF) ||   // 汉字
                        (c >= 'A' && c <= 'Z') ||          // 大写字母（使用字符常量更清晰）
                        (c >= 'a' && c <= 'z') ||          // 小写字母
                        (c >= '0' && c <= '9');            // 数字
                });
            if (!has_encode)
            {
                return 0;
            }

            return 1;
        }
        #endregion

    }
}
