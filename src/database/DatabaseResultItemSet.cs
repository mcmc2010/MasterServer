
using System.Collections;
using System.Reflection;

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
    public class DatabaseResultItemSet : IDictionary<string, DatabaseResultItem>
    {
        private List<DatabaseResultItem> _result = new List<DatabaseResultItem>();


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

            //     var item = _result.FirstOrDefault(i => i.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
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
            //     var index = _result.FindIndex(i => i.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
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

                var item = _result.FirstOrDefault(i => i.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
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
                throw new ArgumentException("Key and value's key must be the same.");
            }
            if (ContainsKey(key)) {
                throw new ArgumentException("An item with the same key already exists.");
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
            return _result.Any(item => item.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
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
            var item = _result.FirstOrDefault(i => i.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
            if (item != null) {
                return _result.Remove(item);
            }
            return false;
        }

        public bool Remove(KeyValuePair<string, DatabaseResultItem> item)
        {
            var existing = _result.FirstOrDefault(i => i.name.Contains(item.Key, StringComparison.CurrentCultureIgnoreCase));
            if (existing != null && EqualityComparer<DatabaseResultItem>.Default.Equals(existing, item.Value)) {
                return _result.Remove(existing);
            }
            return false;
        }

        public bool TryGetValue(string key, out DatabaseResultItem value)
        {
            var item = _result.FirstOrDefault(i => i.name.Contains(key, StringComparison.CurrentCultureIgnoreCase));
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