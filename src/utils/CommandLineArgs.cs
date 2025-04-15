
using YamlDotNet.Core;

namespace AMToolkits.Utility
{
    public class CommandLineArgs
    {
        /// <summary>
        /// 解析第一个
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string[]? FirstParser(object arguments)
        {
            string[] values = new string[] {};
            if(arguments == null) {
                return values;
            }

            var type = arguments.GetType();
            if(type == typeof(object[]))
            {
                object[]? args = arguments as object[];
                if(args == null || args.Length == 0) {
                    return values;
                }
                else
                {
                    return values = args[0] as string[] ?? values;
                }
            }
            else
            {
                return new string[] {
                    arguments as string ?? ""
                };
            }
        }
    }
}