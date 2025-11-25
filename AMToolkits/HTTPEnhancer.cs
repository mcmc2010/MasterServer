
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using AMToolkits.Extensions;

namespace AMToolkits.Net
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class HTTPEnhancerItem : JsonSerializerItem<HTTPEnhancerItem>, IDisposable
    {
        public string key = "";
        public string name = "";
        public long timestamp = 0;
        public float time = 0.0f;

        public int counter = 0;     // 请求计数
        public int repetition = 0;  // 重复计数

        public bool IsFinal => this.timestamp == 0;
        public bool IsPrint => _is_print;

        internal bool _is_print = false;
        private int _ref_dispose = 0;

        public bool OnCreate()
        {
            if (_ref_dispose > 0)
            {
                return false;
            }

            Interlocked.Exchange(ref _ref_dispose, 1);
            return true;
        }
        
        protected virtual void OnDispose()
        {
            Interlocked.Exchange(ref _ref_dispose, 0);
        }

        public void Dispose()
        {
            if (_ref_dispose >= 1)
            {
                this.OnDispose();

                HTTPEnhancer.EventFinal(this);
            }
        }


    }
    
    /// <summary>
    /// 优化连接请求，避免重复多次快速请求，带来的服务器压力
    /// </summary>
    public class HTTPEnhancer 
    {
        /// <summary>
        /// 每个项目超时时间
        /// </summary>
        public static TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(30 * 1000);
        public static TimeSpan TimeExpired { get; set; } = TimeSpan.FromSeconds(30 * 60);
        public static TimeSpan TimeCleanInterval { get; set; } = TimeSpan.FromSeconds(10);

        private static long _time_last_clear = 0;
        private static ConcurrentDictionary<string, HTTPEnhancerItem> _items = new ConcurrentDictionary<string, HTTPEnhancerItem>();

        /// <summary>
        /// 
        /// </summary>
        public static System.Action<string>? OnLogOutput = null;

        public HTTPEnhancer()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static string MakeKey(string name, string? id)
        {
            string normalized = name.Trim().ToLower();
            string key = Regex.Replace(normalized, @"\s+", "_");

            if (!string.IsNullOrWhiteSpace(id))
            {
                key = $"{key}:{id.Trim().ToLower()}";
            }

            return key;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CleanAllExpired()
        {

            float elapsed = 0.0f;
            if (_time_last_clear > 0)
            {
                elapsed = (AMToolkits.Utils.GetLongTimestamp() - _time_last_clear) * 0.001f;
            }

            if (elapsed == 0.0f || elapsed >= TimeCleanInterval.TotalSeconds)
            {
                //
                foreach (var kv in _items)
                {
                    if (kv.Value.timestamp + TimeExpired.TotalMilliseconds < AMToolkits.Utils.GetLongTimestamp())
                    {
                        _items.TryRemove(kv.Key, out _);
                    }
                }

                _time_last_clear = AMToolkits.Utils.GetLongTimestamp();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="timestamp"></param>
        /// <param name="is_print"></param>
        /// <returns></returns>
        private static HTTPEnhancerItem? NewItem(string key, string name, long timestamp, bool is_print)
        {
            var item = new HTTPEnhancerItem
            {
                key = key,
                name = name,
                timestamp = timestamp,
                _is_print = is_print
            };

            if (!item.OnCreate())
            {
                return null;
            }

            _items.TryAdd(key, item);
            Interlocked.Increment(ref item.counter);

            return item;
        }

        public static HTTPEnhancerItem? Event(string name, string? id = null, bool is_print = false)
        {
            CleanAllExpired();

            //
            string key = MakeKey(name, id);

            long timestamp = AMToolkits.Utils.GetLongTimestamp();

            //
            HTTPEnhancerItem? item;
            _items.TryGetValue(key, out item);
            if (item != null)
            {
                item._is_print = is_print;
                if(CheckItem(item, timestamp) <= 0)
                {
                    ResultItem(key, item, 0, is_print);
                    return null;
                }

                if (!item.OnCreate())
                {
                    ResultItem(key, item, 0, is_print);
                    return null;
                }
            }
            else
            {
                item = NewItem(key, name, timestamp, is_print);
                if(item == null)
                {
                    ResultItem(key, item, -1, is_print);
                    return null;
                }
            }

            //
            Interlocked.Increment(ref item.counter);
            item.timestamp = timestamp;
            return item;
        }


        private static int CheckItem(HTTPEnhancerItem item, long timestamp)
        {
            // 检查项状态
            if (item.IsFinal)
            {
                // 最终状态不处理
            }
            else if (item.timestamp + Timeout.TotalMilliseconds <= timestamp)
            {
                item.Dispose();
            }
            else
            {
                return 0;
            }

            // 重新初始化项
            return 1;
        }
        
        private static void ResultItem(string key, HTTPEnhancerItem? item, int code = 0, bool is_print = false)
        {
            if (item != null)
            {
                Interlocked.Increment(ref item.repetition);
            }

            if (item?._is_print == true || is_print)
            {
                OnLogOutput?.Invoke(item?.JsonString ?? $"{{ \"key\":\"{key}\", \"code\":\"{code}\" }}");
            }
        }
        
        internal static void EventFinal(HTTPEnhancerItem? item)
        {
            string key = item?.key.Trim().ToLower() ?? "";
            if (item == null || key.IsNullOrWhiteSpace() || item.IsFinal)
            {
                return;
            }

            item.time = System.Math.Max(0.01f, AMToolkits.Utils.DiffTimestamp(item.timestamp, AMToolkits.Utils.GetLongTimestamp()));
            item.timestamp = 0;
            Interlocked.Exchange(ref item.repetition, 0);
        }
    }
}