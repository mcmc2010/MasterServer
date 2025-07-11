
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
            if (dict != null)
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
            if (dict != null && pairs != null)
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
            if (dict != null && pairs != null)
            {
                foreach (var v in pairs)
                {
                    if (v.Key == null) { continue; }
                    dict.Set(v.Key, v.Value);
                }
            }
        }


#pragma warning restore CS8602

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name=""></typeparam>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static Dictionary<TKey, object?>? ToDictionaryObject<TKey>(this IDictionary<TKey, object?>? dict)
                            where TKey : notnull
        {
            if(dict == null) { return null; }

            var list = new Dictionary<TKey, object?>();
            foreach (var v in dict)
            {
                if(v.Value is System.Text.Json.JsonElement elem)
                list[v.Key] = elem.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => elem.GetString(),
                    System.Text.Json.JsonValueKind.Number => elem.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    System.Text.Json.JsonValueKind.Undefined => null,
                    System.Text.Json.JsonValueKind.Object => elem,
                    System.Text.Json.JsonValueKind.Array => elem,
                    _ => elem
                };
            }
            return list;
        }

        public static Dictionary<TKey, TVal>? ToDictionaryFromJson<TKey, TVal>(this string? json)
                            where TKey : notnull
        {
            if (json == null || json.Trim().Length == 0)
            {
                return null;
            }

            try
            {
                var o = System.Text.Json.JsonSerializer.Deserialize<Dictionary<TKey, TVal>>(json,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                IgnoreReadOnlyFields = true,
                                IncludeFields = true,
                                // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                            });
                return o;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e.Message}");
                return null;
            }
        }

    }
}