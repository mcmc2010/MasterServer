using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_64
using UnityEngine;
#endif

namespace Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string MakeUserUID(string key = "")
        {
#if UNITY_64
            string uid = UnityEngine.SystemInfo.deviceUniqueIdentifier;
#else
            var sb = new System.Text.StringBuilder();
    
            // 系统信息
            sb.Append(System.Environment.MachineName);
            sb.Append("_"+System.Environment.UserName);
            sb.Append("_"+System.Environment.OSVersion.VersionString);
            string uid = sb.ToString();

            var sha = System.Security.Cryptography.SHA256.Create();
            // 字符串转 byte 数组
            byte[] hash = System.Text.Encoding.UTF8.GetBytes(uid);
            uid = System.Convert.ToHexString(hash).ToUpper();
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

            uid = "";
            for (int i = 0; i < digest.Length; i++)
            {
                uid += string.Format("{0:X2}", digest[i]);
            }
            return uid;
        }

        /// <summary>
        /// 设计容错率为每秒钟30000的ID
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID_10(DateTime? now = null)
        {

            char[] chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

            // 时间戳
            DateTime epoch = new DateTime(1990, 1, 1);
            TimeSpan ts = now.GetValueOrDefault(DateTime.Now).ToUniversalTime() - epoch;
            int timestamp = (int)(ts.TotalSeconds % 1000000000) + 1000000000;


            // 随机数
            var random = new System.Random();
            int rand = random.Next(10000, 35000);

            string value = string.Format("{0:D10}{1:D5}", timestamp, rand);

            value = "";
            for (int i = rand; i > 0;)
            {
                value += chars[i % chars.Length];
                i = i / chars.Length;
            }

            for (int i = timestamp; i > 0;)
            {
                value += chars[i % chars.Length];
                i = i / chars.Length;
            }

            if(value.Length < 10)
            {
                rand = random.Next(0, chars.Length);
                value += chars[rand];
                value += "X";
                value = value.Substring(0, 10);
            }
            return value;
        }


        public static int GetTimestamp()
        {
            var now = DateTime.UtcNow;
            var dt = new DateTime(1970, 1, 1);
            double t = (now - dt).TotalSeconds;
            return (int)t;
        }

        public static long GetLongTimestamp()
        {
            var now = DateTime.UtcNow;
            var dt = new DateTime(1970, 1, 1);
            double t = (now - dt).TotalMilliseconds;
            return (long)t;
        }

        public static float GetTimeDelay(long A, long B, float max = 0.460f)
        {
            float delay = 0.030f;
            if(A > 0)
            {
                delay = (B - A) * 0.001f;
                if(delay >= max) {
                    delay = max;
                }
            }
            return delay;
        }
    }
}