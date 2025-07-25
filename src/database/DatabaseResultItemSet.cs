
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseResultItem
    {
        public string name = "";
        public object? value = null;

        public string String {
            get {
                return AsString();
            }
        }

        public long Number {
            get {
                return AsNumber();
            }
        }

        public DateTime? Date {
            get {
                return AsDateTime();
            }
        }

        public string AsString(string def = "")
        {
            if(value == null || value.GetType() != typeof(System.String)) {
                return def;
            }
            return (string)value;
        }

        public long AsNumber(long def = 0)
        {
            if(value == null) {
                return def;
            }

            switch(value)
            {
                case uint unv: return unv;
                case int nv: return nv;
                case UInt64 unv64: return (long)unv64;
                case Int64 nv64: return (long)nv64;
            }

            return def;
        }

        public DateTime? AsDateTime(DateTime? def = null)
        {
            if(value == null) {
                return def;
            }

            switch(value)
            {
                case uint unv: return DateTimeOffset.FromUnixTimeSeconds(unv).DateTime;
                case int nv: return DateTimeOffset.FromUnixTimeSeconds(nv).DateTime;
                case UInt64 unv64: return DateTimeOffset.FromUnixTimeMilliseconds((long)unv64).DateTime;
                case Int64 nv64: return DateTimeOffset.FromUnixTimeMilliseconds((long)nv64).DateTime;
                case DateTime dt: return dt;
            }
            return def;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DatabaseResultJsonConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type convert, JsonSerializerOptions options)
        {
            JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;
            // 提取内层 name 值
            JsonElement value;
            root.TryGetProperty("value", out value);
            if(value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                if (typeof(T) == typeof(DateTime?))
                {
                    return default; // 返回 null
                }
                return default(T);
            }
            if(convert == typeof(string))
            {
                return (T?)(object?)value.GetString();
            }
            else if(convert == typeof(System.Int32))
            {
                return (T?)(object?)value.GetInt32();
            }
            else if(convert == typeof(System.Int64))
            {
                return (T?)(object?)value.GetInt64();
            }
            else if(convert == typeof(System.Double))
            {
                return (T?)(object?)value.GetDouble();
            }
            else if(convert == typeof(System.Boolean))
            {
                return (T?)(object?)value.GetBoolean();
            }
            else if(convert == typeof(System.DateTime))
            {
                return (T?)(object?)value.GetDateTime();
            }
            //return System.Text.Json.JsonSerializer.Deserialize(value.GetRawText(), convert, options);
            return default(T);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DatabaseResultItemSet : IDictionary<string, DatabaseResultItem>
    {
        private List<DatabaseResultItem> _result = new List<DatabaseResultItem>();

        public T? To<T>() where T : new()
        {
            string json = this.ToJson();
            T? o = System.Text.Json.JsonSerializer.Deserialize<T>(json, 
                            new System.Text.Json.JsonSerializerOptions
                            {
                                IncludeFields = true, 
                                IgnoreReadOnlyFields = true,
                                IgnoreReadOnlyProperties = true,
                                // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                // ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                                Converters = { 
                                    new DatabaseResultJsonConverter<string>(), 
                                    new DatabaseResultJsonConverter<System.Int32>(),
                                    new DatabaseResultJsonConverter<System.Int64>(),
                                    new DatabaseResultJsonConverter<System.Double>(),
                                    new DatabaseResultJsonConverter<System.Boolean>(),
                                    // 同时添加DateTime和DateTime?的转换器
                                    new DatabaseResultJsonConverter<DateTime>(),
                                    new DatabaseResultJsonConverter<DateTime?>(),
                                }
                            });
            return o;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            string json = System.Text.Json.JsonSerializer.Serialize<DatabaseResultItemSet>(this,                                
                            new System.Text.Json.JsonSerializerOptions
                            {
                                IncludeFields = true, 
                                IgnoreReadOnlyFields = true,
                                IgnoreReadOnlyProperties = true,
                                // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                // ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                            });
            return json;
        }

        /// <summary>
        /// 包含 Key 属性, 此处需要null，并且为只读，添加项目只能用Add
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public DatabaseResultItem? this[string key]
        {
            // get
            // {
            //     if(string.IsNullOrEmpty(key.Trim())) {
            //         throw new KeyNotFoundException($"The key is null.");
            //     }

            //     var item = _result.FirstOrDefault(i => i.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            //     if (item == null)
            //         throw new KeyNotFoundException($"The key '{key}' was not found.");
            //     return item;
            // }
            // set
            // {
            //     if(string.IsNullOrEmpty(key.Trim())) {
            //         throw new KeyNotFoundException($"The key is null.");
            //     }
            //     if (key != value.name) {
            //         throw new ArgumentException("Key and value's key must be the same.");
            //     }
            //     var index = _result.FindIndex(i => i.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            //     if (index >= 0) {
            //         _result[index] = value;
            //     } else {
            //         _result.Add(value);
            //     }
            // }
            get 
            {
                if(string.IsNullOrEmpty(key.Trim())) {
                    return null;
                }

                var item = _result.FirstOrDefault(i => i.name.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (item == null) {
                    return null;
                }
                return item;
            }
            set 
            {
                throw new KeyNotFoundException($"The key '{key}' not set readonly attribute.");
            }
        }

        //
        public ICollection<string> Keys => _result.Select(item => item.name).ToList();
        public ICollection<DatabaseResultItem> Values => _result.ToList();
        public int Count => _result.Count;

        public bool IsReadOnly => false;

        public void Add(string key, DatabaseResultItem value)
        {
            if (key != value.name) {
                throw new ArgumentException($"Key({key}) and value's key must be the same.");
            }
            if (ContainsKey(key)) {
                throw new ArgumentException($"An item with the same key({key}) already exists.");
            }
            _result.Add(value);
        }

        public void Add(KeyValuePair<string, DatabaseResultItem> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _result.Clear();
        }
        
        public bool Contains(KeyValuePair<string, DatabaseResultItem> item)
        {
            return TryGetValue(item.Key, out var value) && EqualityComparer<DatabaseResultItem>.Default.Equals(value, item.Value);
        }

        public bool ContainsKey(string key)
        {
            return _result.Any(item => item.name.Equals(key, StringComparison.OrdinalIgnoreCase));
        }


        public void CopyTo(KeyValuePair<string, DatabaseResultItem>[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Length - arrayIndex < _result.Count) {
                throw new ArgumentException("Destination array is not long enough.");
            }
            foreach (var item in _result) {
                array[arrayIndex++] = new KeyValuePair<string, DatabaseResultItem>(item.name, item);
            }
        }

        public IEnumerator<KeyValuePair<string, DatabaseResultItem>> GetEnumerator()
        {
            foreach (var item in _result) {
                yield return new KeyValuePair<string, DatabaseResultItem>(item.name, item);
            }
        }

        public bool Remove(string key)
        {
            var item = _result.FirstOrDefault(i => i.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (item != null) {
                return _result.Remove(item);
            }
            return false;
        }

        public bool Remove(KeyValuePair<string, DatabaseResultItem> item)
        {
            var existing = _result.FirstOrDefault(i => i.name.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
            if (existing != null && EqualityComparer<DatabaseResultItem>.Default.Equals(existing, item.Value)) {
                return _result.Remove(existing);
            }
            return false;
        }

        public bool TryGetValue(string key, out DatabaseResultItem value)
        {
            var item = _result.FirstOrDefault(i => i.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if(item == null) {
                throw new ArgumentNullException(key);
                //return false;
            }
            value = item;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}