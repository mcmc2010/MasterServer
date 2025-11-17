using System.Text.RegularExpressions;



namespace AMToolkits.Statistics
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class StatisticalItem 
    {
        public string name = "";
        public long timestamp = 0;
        // 毫秒
        public float time = 0;
        public float time_min = 0;
        public float time_max = 0;
        // 
        public int count = 0;

        public string JsonString
        {
            get
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize<StatisticalItem>(this, 
                                new System.Text.Json.JsonSerializerOptions()
                                {
                                    IgnoreReadOnlyFields = true,
                                    IgnoreReadOnlyProperties = true,
                                    IncludeFields = true,
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                });
                    return json;
                } 
                catch (Exception ex)
                {
                    return "(JSON) (StatisticalItem) Error :" + ex.Message;
                }
            }
        }
    }

    public class StatisticalEvent : IDisposable
    {
        private bool _is_print = false;
        private StatisticalItem? _item = null;
        public StatisticalEvent(string name, bool is_print = false)
        {
            _is_print = is_print;

            name = name.Trim().ToLower();
            name = Regex.Replace(name, @"\s+", "_");

            StatisticalManager.Event(_item = new StatisticalItem()
            {
                name = name,
                timestamp = AMToolkits.Utils.GetLongTimestamp(),
                time = 0
            }, false);  
        }

        public void Dispose()
        {
            if(_item != null) {
                if(_item.timestamp > 0) {
                    _item.time = AMToolkits.Utils.DiffTimestamp(_item.timestamp, AMToolkits.Utils.GetLongTimestamp()) * 1000.0f;
                }
                StatisticalManager.Event(_item, true);
                if(_is_print)
                {
                    StatisticalManager.PrintItem(_item);
                }
                _item = null;
            }
        }
    }
}