

namespace AMToolkits
{
    public class CommandLineArgs
    {
        /// <summary>
        /// 解析第一个
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string[] ParserArray(object arguments)
        {
            // 处理 null 情况
            if (arguments == null)
            {
                return Array.Empty<string>();
            }

            // 根据不同类型进行处理
            switch (arguments)
            {
                // 字符串类型：按空格分割并移除空项
                case string s: return s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // 字符串数组：直接返回
                case string[] array: return array;

                // 对象数组：转换为字符串数组
                case object[] vo: return vo.Select(o => o?.ToString() ?? "").ToArray();

                // 可枚举集合：转换为字符串数组
                case IEnumerable<object> oe: return oe.Select(o => o as string ?? "").ToArray();
            }

            // 尝试处理命令行参数格式
            if (arguments is System.Collections.IEnumerable enumerable)
            {
                return enumerable.Cast<object>()
                                .Select(o => o as string ?? "")
                                .ToArray();
            }

            // 最后尝试：转换为字符串并按空格分割
            var v = arguments as string;
            if (v != null)
            {
                return new string[] { v };
            }
            return Array.Empty<string>();
        }

        // 辅助函数：检查字符串是否是选项（以 -- 或 - 开头）
        private static bool IsValueOption(string value)
        {
            return value.StartsWith("--") || value.StartsWith("-");
        }

        public static Dictionary<string, string?> ParserCommandLines(object arguments)
        {
            Dictionary<string, string?> pairs = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);
            if (arguments == null)
            {
                return pairs;
            }

            var args = ParserArray(arguments);
            if (args == null || args.Length == 0)
            {
                return pairs;
            }

            int i = 0;
            while (i < args.Length)
            {
                string current = args[i].Trim();

                // 处理键值对参数（以 -- 或 - 开头）
                if (current.StartsWith("--") || current.StartsWith("-"))
                {
                    // 移除前缀（-- 或 -）
                    string key = current.StartsWith("--")
                        ? current.Substring(2)
                        : current.Substring(1);

                    // 检查下一个元素是否可作为值
                    if (i + 1 < args.Length && !IsValueOption(args[i + 1]))
                    {
                        // 下一个元素是值
                        pairs[key] = args[i + 1];
                        i += 2; // 跳过值和当前键
                    }
                    else
                    {
                        // 没有值，使用空字符串
                        pairs[key] = null;//string.Empty;
                        i++; // 仅移动到下一个元素
                    }
                }
                else
                {
                    // 处理独立参数（不以 -- 或 - 开头）
                    pairs[current] = string.Empty;
                    i++;
                }
            }

            //
            return pairs;
        }

        /// <summary>
        /// 解析第一个
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string[] FirstParser(object arguments)
        {
            string[] values = new string[] { };
            if (arguments == null)
            {
                return values;
            }

            if (arguments is object[] args)
            {
                object? val = null;
                if (args.Length > 0)
                {
                    val = args[0];
                }
                return ParserArray(val ?? "");
            }

            return values.Append(arguments as string ?? "").ToArray();
        }
    }

    public static class CommandLineArgsExtensions
    {
        public static void PrintCommandLineArgs(this string[]? args)
        {
            System.Console.WriteLine(string.Join(System.Environment.NewLine, args ?? new string[] { }));
        }
        public static void PrintCommandLineArgs(this IDictionary<string, string>? args)
        {
            if (args == null)
            {
                args = new Dictionary<string, string>() { };
            }
            
            foreach (var pairs in args)
            {
                System.Console.WriteLine($"{pairs.Key} : {pairs.Value}");
            }
        }
    }
}