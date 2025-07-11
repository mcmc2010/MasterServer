

namespace AMToolkits.Game
{

    [System.Serializable]
    public class GeneralItemData
    {
        public string IID = "";  // 物品实例ID
        public int ID = Game.ItemConstants.ID_NONE;      // 物品ID
        public int Count = 0;   // 物品数量
        public int Type  = (int)Game.ItemType.Default;    // 物品类型
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

        public static GeneralItemData[]? ParseGeneralItem(string? value, int count = 0,
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

        /// <summary>
        /// 获取物品列表中是否设置了特殊道具转换为货币
        /// 仅仅是列表中的第一个其它会忽略
        /// </summary>
        /// <param name="items"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GeneralItemData? GetVirtualCurrency(GeneralItemData[]? items,
                        int id = ItemConstants.ID_NONE)
        {
            if (items == null) { return null; }
            var item = items.FirstOrDefault(v =>
                (id == ItemConstants.ID_NONE || (id > ItemConstants.ID_NONE && v.ID == id)) &&
                (v.ID >= ItemConstants.ID_N0 && v.ID <= ItemConstants.ID_NN));
            // 如果未设置
            if (item?.ID == ItemConstants.ID_GD)
            {
                item.Type = (int)Game.ItemType.Economy;
            }
            else if (item?.ID == ItemConstants.ID_GM)
            {
                item.Type = (int)Game.ItemType.Economy;
            }
            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static GeneralItemData[]? GetGeneralItems(GeneralItemData[]? items)
        {
            if (items == null) { return null; }
            return items.Where(v =>
                v.ID != ItemConstants.ID_GD &&
                v.ID != ItemConstants.ID_GM &&
                v.ID > ItemConstants.ID_NN &&
                v.ID < ItemConstants.ID_N0)
            .ToArray();
        }

    }
}