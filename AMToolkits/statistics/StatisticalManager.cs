

namespace AMToolkits.Statistics
{
    /// <summary>
    /// 
    /// </summary>
    public class StatisticalManager
    {
        private static object _locked = new object() { };
        private static Dictionary<string, StatisticalItem> _items = new Dictionary<string, StatisticalItem>();

        public static System.Action<string>? OnLogOutput = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        internal static void PrintItem(StatisticalItem item)
        {
            if(OnLogOutput != null)
            {
                string content = item.JsonString;
                OnLogOutput.Invoke(content);
            }
            else
            {
                System.Console.WriteLine($"(AMSX) {item.name} : " + item.JsonString);
            }
        } 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public static void Event(StatisticalItem item, bool is_final = false)
        {
            lock(_locked)
            {
                if(_items.TryGetValue(item.name, out StatisticalItem? value) && value != null)
                {
                    if(!is_final) {
                        value.count ++;
                        value.timestamp = item.timestamp;
                    }
                    else
                    {
                        if(value.time_min == 0 || item.time < value.time_min)
                        {
                            value.time_min = item.time;
                        }
                        if(item.time >= value.time_max)
                        {
                            value.time_max = item.time;
                        }
                    
                        value.time = item.time;

                        // final
                        item.count = value.count;
                        item.time_max = value.time_max;
                        item.time_min = value.time_min;
                    }
                }
                else
                {
                    item.count ++;
                    _items.Add(item.name, item);
                }
            }
        }
    }
}