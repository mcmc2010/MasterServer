using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;



namespace AMToolkits.Utility
{
    public class Guid 
    {
        /// <summary>
        /// long 表示最大值 2^63 - 1
        /// </summary>
        public const long LONG_MAX = 9223372036854775807;

        public static string DecToB32(long num)
        {
            string s32 = "0123456789ABCDEFGHJKLMNPRSTVWXYZ";
            char[] chars = s32.ToCharArray();
            int bit = chars.Length;

            string value = "";
            // 从低位到高位依次计算
            for (; num > 0;)
            {
                long n = num % bit;
                value = chars[(int)n] + value;
                num /= bit;
            }
            return value;
        }

        #region Random
        /// <summary>
        /// 
        /// </summary>
        private static readonly object _random_lock = new object();
        private static long _random_index = 0;

        /// <summary>
        /// 随机数为1000 0000 - 9999 9999
        /// </summary>
        /// <returns></returns>
        private static int GenerateRandom()
        {
            lock (_random_lock)
            {
                _random_index++;
            }

            // 随机数
            var random = new System.Random();
            int A = random.Next(1000, 9999);
            int B = random.Next(0, 9999);
            B = (B + (int)(_random_index % 100) ) % 10000;
            return A * 10000 + B;
        }

        public static int Random(int min, int max)
        {
            // 随机数
            int rand = GenerateRandom() % System.Math.Abs(max - min) + min;
            return rand;
        }

        #endregion

        #region UID
        private static long _generator_idx_index = 0;

        /// <summary>
        /// 用于临时校验
        /// 随机6个数字
        /// </summary>
        /// <returns></returns>
        public static int GeneratorID6()
        {
            int value = Guid.Random(100000, 999999);
            return value;
        }

        /// <summary>
        /// 用于临时校验
        /// 随机8个字符，包含数字和字母，字母不区分大小写
        /// 1 099 511 627 776
        ///   999 999 999 999
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID8()
        {
            var now = System.DateTime.UtcNow;
            int AAA = (now.DayOfYear % 8 + 1) * 100 + now.Hour;
            int BB = now.Minute;
            long VA = AAA * 100 + BB;
            long V = VA * 10000 + Guid.Random(10000, 99999);
            V = V * 100 + now.Second;
            
            string value = "XXXX" + DecToB32(V);
            value = value.Substring(value.Length - 8);
            return value;
        }

        /// <summary>
        /// 用于唯一标识
        /// 包含数字和字母，字母不区分大小写
        /// YYY DDDD MM RRRR
        /// 100 2064 47 3529
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID10()
        {
            var now = System.DateTime.UtcNow;
            int AAA = now.Year % 800 + 100;
            int BBBB = now.DayOfYear * 24;
            // 精度到小时
            long V = AAA * 10000 + BBBB;
            int CC = now.Minute;
            int RRRR = Guid.Random(1000, 9999);
            V = V * 100 + CC;
            V = V * 10000 + RRRR;
            int DD = now.Second;
            V = V * 100 + DD;
            string value = "XXXX" + DecToB32(V);
            value = value.Substring(value.Length - 10);
            return value;
        }

        /// <summary>
        /// 用于唯一标识,不建议使用
        /// 包含数字,每小时精度9999
        /// YYY DDDD RRRR C
        /// 100 2064 3599 0
        /// 125 2064 9300 4
        /// </summary>
        /// <returns></returns>
        private static int CheckDigit(string value)
        {
            // 计算校验位
            // 反转字符串以便从右向左处理
            char[] chars = value.ToCharArray();
            Array.Reverse(chars);

            int sum = 0;
            bool e = true;
            for (int i = 0; i < chars.Length; i++)
            {
                int digit = chars[i] - '0';
                if (e)
                {
                    // 偶数位置：乘以2并处理进位
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                e = !e; // 切换位置标记
            }

            int c = (10 - (sum % 10)) % 10;
            return c;
        }

        /// <summary>
        /// 30年周期
        /// AA BBBB MM RRRR
        /// 弱唯一标识，比如账号ID
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID12N()
        {
            var now = System.DateTime.UtcNow;
            int AA = now.Year % 30;
            if(AA < 10) { AA = AA + 10; }

            int BBBB = now.DayOfYear * 24;
            // 精度到小时
            long V = AA * 10000 + BBBB;

            int MM = now.Minute / 12;
            int MS = now.Millisecond / 100;
            V = V * 100 + MM * 10 + MS;

            int RRRR = Guid.Random(1000, 9999);
            V = V * 10000 + RRRR;

            string value = V.ToString();
            return value;
        }

        /// <summary>
        /// 建议使用16位数字标识
        /// 产出如物品ID
        /// 125 2064 0300 6364 7
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID16N()
        {
            var now = System.DateTime.UtcNow;
            int AAA = now.Year % 100 + 100;
            int BBBB = now.DayOfYear * 24;
            // 精度到小时
            long V = AAA * 10000 + BBBB;
            int CCC = (now.Minute * 60 + now.Second) / 5;
            V = V * 1000 + CCC;
            V = V * 10 + now.Millisecond / 100;
            int RRRR = Guid.Random(1000, 9999);
            V = V * 10000 + RRRR;

            string value = V.ToString();
            value = value + CheckDigit(value).ToString();
            return value;
        }

        /// <summary>
        /// 建议使用18位数字标识
        /// 例如：日志序号，流水单号
        /// 125 2112 0515 7066 03 1
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID18N()
        {
            var now = System.DateTime.UtcNow;
            int AAA = now.Year % 100 + 100;
            int BBBB = now.DayOfYear * 24;
            // 精度到小时
            long V = AAA * 10000 + BBBB;
            int CCC = (now.Minute * 60 + now.Second) / 5;
            V = V * 1000 + CCC;
            int RRRR = Guid.Random(1000, 9999);
            V = V * 10000 + RRRR;
            V = V * 100 + now.Millisecond / 10;
            V = V * 10 + (++_generator_idx_index % 10);
            string value = V.ToString();
            value = value + CheckDigit(value).ToString();
            return value;
        }

        /// <summary>
        /// 建议使用12位
        /// 精度为0.01秒 9999
        /// 999 2064 2136 4736 96
        /// 2RPX 9DRN 3H48
        /// </summary>
        /// <returns></returns>
        public static string GeneratorID12()
        {
            var now = System.DateTime.UtcNow;
            int AAA = now.Year % 800 + 100;
            int BBBB = now.DayOfYear * 24;
            // 精度到小时
            long V = AAA * 10000 + BBBB;
            int CCCC = now.Minute * 60 + now.Second;
            V = V * 10000 + CCCC;
            int RRRR = Guid.Random(1000, 9999);
            V = V * 10000 + RRRR;
            int DD = now.Millisecond / 10; // FPS 99
            V = V * 100 + DD;

            string value = "XXXX" + DecToB32(V);
            value = value.Substring(value.Length - 12);
            return value;
        }

        #endregion
    }
}