

namespace AMToolkits.Game
{
    [System.Serializable]
    public class GeneralItemData
    {
        public int ID;
        public int Count;
        public List<string> Attributes = new List<string>();
        public GeneralItemData()
        {

        }
    }

    public static class ItemUtils
    {
        /// <summary>
        /// 解析物品列表
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string[] ParseItemValues(string? value, string separator = ",")
        {
            var values = (value ?? "").Split(separator)
                        .Select(v => v.Trim())
                        .Where(v => v.Length > 0).ToArray();
            return values;
        }

        /// <summary>
        /// 解析单个物品值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string[] ParseItemValue(string? value, string separator = "|")
        {
            var values = (value ?? "").Split(separator).Select(v => v.Trim()).ToArray();
            return values;
        }

        public static GeneralItemData[]? ParseGeneralItemData(string? value, int count = 0,
                            string separator = ",|")
        {
            var items = ItemUtils.ParseItemValues(value, separator[0].ToString());
            if (items == null)
            {
                return null;
            }

            List<GeneralItemData> list = new List<GeneralItemData>();
            foreach (var item in items)
            {
                var values = ItemUtils.ParseItemValue(item, separator[1].ToString());
                if (values.Length == 0) { continue; }

                var data = new GeneralItemData();
                if (!int.TryParse(values[0], out data.ID))
                {
                    continue;
                }

                // 没有设置，按默认值处理
                if (values.Length <= 1)
                {
                    data.Count = count;
                }
                else if (values.Length > 1 && !int.TryParse(values[1], out data.Count))
                {
                    continue; //解析错误忽略
                }

                // 修改：避免索引越界
                if (values.Length > 2)
                {
                    data.Attributes.AddRange(values.Skip(2).ToArray());
                }
                list.Add(data);
            }

            return list.ToArray();
        }

    }
}