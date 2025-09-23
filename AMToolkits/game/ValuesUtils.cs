
namespace AMToolkits.Game
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class GeneralValueData
    {
        public int ID = ValuesUtils.ID_NONE;
        public List<string> values = new List<string>();
    }


    /// <summary>
    /// 
    /// </summary>
    public static class ValuesUtils
    {
        public const int ID_NONE = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string? ToValues(string[]? values, string separator = ",")
        {
            if (values == null) { return null; }
            var value = string.Join(separator, values.Select(v => v.Trim()).Where(v => v.Length > 0));
            return value;
        }

        public static GeneralValueData[]? ToGeneralValues(IEnumerable<string?>? values, string separator = "|")
        {
            if (values == null) { return null; }


            List<GeneralValueData> list = new List<GeneralValueData>();
            foreach (var item in values)
            {
                var vs = ParseValues(item);
                if (vs.Length == 0) { continue; }

                var data = new GeneralValueData();
                if (!int.TryParse(vs[0], out data.ID))
                {
                    continue;
                }

                if (vs.Length > 2)
                {
                    data.values.AddRange(vs.Skip(1).ToArray());
                }
                list.Add(data);
            }
            return list.ToArray();
        }

        /// <summary>
        /// 解析列表
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string[] ParseValues(string? value, string separator = ",")
        {
            var values = (value ?? "").Split(separator)
                        .Select(v => v.Trim())
                        .Where(v => v.Length > 0).ToArray();
            return values;
        }


    }
}