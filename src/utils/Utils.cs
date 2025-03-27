#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
#define USE_UNITY_BUILD
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if USE_UNITY_BUILD
using UnityEngine;
#endif

namespace AMToolkits.Utility
{
    /// <summary>
    /// 
    /// </summary>
    public static class Utils
    {
        public const float NETWORK_DELAY_MAX = 460 * 0.001f;
        public const float NETWORK_DELAY_MIN = 10 * 0.001f;

        #region  DateTime
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
        public static float DiffTimestamp(long A,  long B)
        {
            return (float)(B - A) * 0.001f;
        }
        #endregion

        #region String
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[] bytes)
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

        #endregion

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
            string uid = $"{System.Environment.MachineName}_{System.Environment.UserName}_{System.Environment.OSVersion.VersionString}";
            uid = uid + "_" + System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            var sha = System.Security.Cryptography.SHA256.Create();
            // 字符串转 byte 数组
            byte[] hash = System.Text.Encoding.UTF8.GetBytes(uid);
            hash = sha.ComputeHash(hash);
            uid = hash.ToHexString();
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
    }
}