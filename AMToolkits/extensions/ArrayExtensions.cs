


namespace AMToolkits.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ArrayExtensions
    {
        public static DateTime? AsDateTime(this object? o)
        {
            DateTime? dt = AsUTCDateTime(o);
            if (dt.HasValue)
            {
                return dt.Value.ToLocalTime();
            }
            return null;
        }

        public static DateTime? AsUTCDateTime(this object? o)
        {
            if (o == null)
            {
                return null;
            }

            DateTime t0 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (o is long nl)
            {
                return nl > 4_294_967_295 ? t0.AddMilliseconds(nl) : t0.AddSeconds(nl);
            }
            else if (o is int ni)
            {
                return t0.AddSeconds(ni);
            }
            else if (o is string s)
            {
                if (DateTime.TryParse(s,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                                out var dt))
                {
                    return dt.ToUniversalTime();
                }
                else if (long.TryParse(s, out long n))
                {
                    return n > 4_294_967_295 ? t0.AddMilliseconds(n) : t0.AddSeconds(n);
                }
            }
            else if (o is System.Text.Json.JsonElement elem)
            {
                switch (elem.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Number:
                        {
                            if (elem.TryGetInt64(out long vl))
                            {
                                return vl > 4_294_967_295 ? t0.AddMilliseconds(vl) : t0.AddSeconds(vl);
                            }
                            break;
                        }
                    case System.Text.Json.JsonValueKind.String:
                        {
                            s = elem.GetString() ?? "";
                            if (s.Length > 0 && DateTime.TryParse(s,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                                out DateTime dt))
                            {
                                return dt.ToUniversalTime();
                            }
                            break;
                        }
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this object[]? arr)
        {
            return arr == null || arr.Length == 0;
        }


    }
}