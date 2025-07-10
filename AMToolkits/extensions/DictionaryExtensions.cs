
namespace AMToolkits.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<TKey, TVal>(this IDictionary<TKey, TVal>? dict)
        {
            return dict == null || dict.Count == 0;
        }

#pragma warning disable CS8602
        public static void Set<TKey, TVal>(this IDictionary<TKey, TVal>? dict, TKey key, TVal val)
        {
            if (!dict.IsNullOrEmpty())
            {
                if (dict.ContainsKey(key))
                {
                    dict[key] = val;
                }
                else
                {
                    dict.Add(key, val);
                }
            }
        }

        public static TVal? Get<TKey, TVal>(this IDictionary<TKey, TVal>? dict, TKey key, TVal? def = default(TVal))
        {
            if (dict.IsNullOrEmpty()) { return def; }
            if (!dict.ContainsKey(key)) { return def; }

            TVal? val = def;
            if (!dict.TryGetValue(key, out val))
            {
                return def;
            }
            return val;
        }

        /// <summary>
        /// 这将覆盖已有Keys
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="dict"></param>
        /// <param name="pairs"></param>
        public static void AddRange<TKey, TVal>(this IDictionary<TKey, TVal>? dict, IEnumerable<KeyValuePair<TKey, TVal>>? pairs)
        {
            if (!dict.IsNullOrEmpty() && pairs != null)
            {
                foreach (var v in pairs)
                {
                    if (v.Key == null) { continue; }
                    dict.Set(v.Key, v.Value);
                }
            }
        }

        public static void AddRange<TKey, TVal>(this IDictionary<TKey, TVal>? dict, IDictionary<TKey, TVal>? pairs)
        {
            if (!dict.IsNullOrEmpty() && pairs != null)
            {
                foreach (var v in pairs)
                {
                    if (v.Key == null) { continue; }
                    dict.Set(v.Key, v.Value);
                }
            }
        }


#pragma warning restore CS8602
    }
}