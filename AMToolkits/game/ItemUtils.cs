


namespace AMToolkits.Game
{

    [System.Serializable]
    public class GeneralItemData
    {
        public bool Enabled = true;
        public string IID = "";  // 物品实例ID
        public int NID = -1;     // 每批次物品的唯一ID，用来对齐数据传递
        public int ID = Game.ItemConstants.ID_NONE;      // 物品ID
        public int Count = 0;   // 物品数量
        public int Type  = (int)Game.ItemType.Default;    // 物品类型
        public List<string> Attributes = new List<string>();

        /// <summary>
        /// 模版数据是临时数据，不需要序列化
        /// </summary>
        private AMToolkits.Utility.ITableData? _template_data = null;
        public string ItemID { get { return $"id_{this.ID:D}"; } }

        public bool SetItemID(string id)
        {
            try
            {
                var values = id.Split("_");
                if (values.Length > 1)
                {
                    ID = System.Convert.ToInt32(values[1]);
                }
                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception : SetItemID ({id}) {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 模版数据关联
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="template_data"></param>
        public void InitTemplateData<T>(T? template_data) where T : AMToolkits.Utility.ITableData
        {
            if (template_data?.Id != this.ID)
            {
                return;
            }
            this._template_data = template_data;
        }
        public T? GetTemplateData<T>() where T : AMToolkits.Utility.ITableData
        {
            return (T?)this._template_data;
        }

        public GeneralItemData()
        {

        }
    }

    public static class ItemUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string? ToItemValue(GeneralItemData? item, string separator = "|")
        {
            if (item == null) { return null; }
            return $"{item.ID}{separator}{item.Count}{separator}IID{item.IID}";
        }

        public static string[]? ToItemValues(GeneralItemData[]? items, string separator = ",")
        {
            if (items == null) { return null; }
            var values = items.Select(v => ToItemValue(v) ?? "").ToArray();
            return values;
        }

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

                var iid = values.FirstOrDefault(v => v.StartsWith("IID"));
                if (iid != null)
                {
                    data.IID = iid.Substring(3);
                    values = values.Where(v => iid != v).ToArray();
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
        /// 解析属性字符串
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseAttributeValues(string? attributes, string separator = ",|")
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var items = ItemUtils.ParseItemValues(attributes, separator[0].ToString());
            if (items == null)
            {
                return pairs;
            }

            foreach (var item in items)
            {
                var values = ItemUtils.ParseItemValue(item, separator[1].ToString());
                if (values.Length == 0) { continue; }

                string name = values[0];
                string value = "";
                if (values.Length > 1)
                {
                    value = values[1].Trim();
                }

                pairs.Add(name.ToLower(), value);
            }
            return pairs;
        }

        public static string ToAttributeValues(Dictionary<string, string> pairs, string separator = ",|")
        {
            List<string> items = new List<string>();
            foreach (var v in pairs)
            {
                items.Add($"{v.Key}{separator[1].ToString()}{v.Value}");
            }
            string attributes = string.Join(separator[0].ToString(), items);
            return attributes;
        }

        /// <summary>
        /// 是否为虚拟货币物品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool HasVirtualCurrency(int id = ItemConstants.ID_NONE)
        {
            return (id > ItemConstants.ID_NONE) && (id >= ItemConstants.ID_N0 && id <= ItemConstants.ID_NN);
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
        /// 获取物品（不包括货币或等效虚拟道具）
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static GeneralItemData[]? GetGeneralItems(GeneralItemData[]? items)
        {
            if (items == null) { return null; }

            return items.Where(v =>
                (v.ID != ItemConstants.ID_GD && v.ID != ItemConstants.ID_GM) &&
                (v.ID > ItemConstants.ID_NN || v.ID < ItemConstants.ID_N0))
            .ToArray();
        }


        /// <summary>
        /// 获取折扣价
        /// </summary>
        /// <param name="price"></param>
        /// <param name="discount"></param>
        /// <returns></returns>
        public static float GetDiscountPrice(float price, float discount)
        {
            // 计算折扣 
            if (discount > 1.0f)
            {
                discount = discount * 0.01f;
            }
            if (discount >= 1.0f)
            {
                discount = 1.0f;
            }

            // 小于10.0f，不计算折扣
            // 折扣为0，或者-1不参与折扣，保持原价
            // 1.0折扣代表原价出售
            if (price <= 10.0f || discount <= 0.0f || discount >= 1.0f)
            {
                return price;
            }

            decimal discount_price = System.Math.Round((decimal)price * (decimal)discount, 2);
            if (discount_price < 1.00m)
            {
                discount_price = System.Math.Round((decimal)price, 2);
            }
            return (float)discount_price;
        }

        /// <summary>
        /// 优惠价
        /// </summary>
        /// <param name="price"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float GetReductionsPrice(float price, float value)
        {
            float amount = price - value;
            if (amount < 10.0f)
            {
                return price;
            }

            return amount;
        }

    }
}