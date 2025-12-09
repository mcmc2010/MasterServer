using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AMToolkits.Extensions;


namespace AMToolkits
{
    /// <summary>
    /// 需要缓冲的需要派生此接口
    /// </summary>
    public interface IMemoryCachedData
    {

    }
    

    [System.Serializable]
    public class MemoryCachedItem : JsonSerializerItem<MemoryCachedItem>, IDisposable
    {
        public string key = "";
        public string? value = "";
        public long timestamp = 0;

        public void Dispose()
        {
            
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MemoryCached
    {
        /// <summary>
        /// 30 秒后过期
        /// </summary>
        public static TimeSpan TimeExpired { get; set; } = TimeSpan.FromSeconds(30);

        private static ConcurrentDictionary<string, MemoryCachedItem?> _items = new ConcurrentDictionary<string, MemoryCachedItem?>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static string? ToJson(object o)
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(o,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        IgnoreReadOnlyFields = true,
                        IgnoreReadOnlyProperties = true,
                        IncludeFields = true,
                        // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                                                                          // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                    });
                return json;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MemoryCached : {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T? FromJson<T>(string? json)
        {
            if (json == null || json.IsNullOrWhiteSpace())
            {
                return default(T);
            }
            
            try
            {
                var o = System.Text.Json.JsonSerializer.Deserialize<T>(json,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                }
                );
                if (o == null)
                {
                    return default(T);
                }
                return o;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"MemoryCached : {ex.Message}");
                return default(T);
            }
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

        public static int Update(string name, string? id, object o)
        {
            return _Update(MakeKey(name, id), ToJson(o));
        }

        public static int Update(string key, object o)
        {
            return _Update(key, ToJson(o));
        }

        public static T? Get<T>(string name, string? id, bool check_expired = true)
        {
            string key = MakeKey(name, id);

            var item = _GetItem(key);
            if (item == null)
            {
                return default(T);
            }

            if (check_expired && item.timestamp + TimeExpired.TotalMilliseconds <= AMToolkits.Utils.GetLongTimestamp())
            {
                _items.TryRemove(item.key, out _);
                return default(T);
            }

            return FromJson<T>(item.value);
        }

        public static T? Get<T>(string key, bool check_expired = true)
        {
            var item = _GetItem(key);
            if (item == null)
            {
                return default(T);
            }

            if (check_expired && item.timestamp + TimeExpired.TotalMilliseconds <= AMToolkits.Utils.GetLongTimestamp())
            {
                _items.TryRemove(item.key, out _);
                return default(T);
            }

            return FromJson<T>(item.value);
        }

        private static MemoryCachedItem? _GetItem(string key)
        {
            key = key.Trim().ToLower() ?? "";
            if (key.IsNullOrWhiteSpace())
            {
                return null;
            }

            MemoryCachedItem? item = null;
            if (!_items.TryGetValue(key, out item) || item?.key != key)
            {
                return null;
            }

            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int _Update(string key, string? value = null)
        {
            key = key.Trim().ToLower() ?? "";
            if (key.IsNullOrWhiteSpace())
            {
                return -1;
            }

            long timestamp = AMToolkits.Utils.GetLongTimestamp();

            var item = new MemoryCachedItem
            {
                key = key,
                value = value,
                timestamp = timestamp,
            };

            _items.AddOrUpdate(key, item, (k, v) => item);

            return 0;
        }
    }
}