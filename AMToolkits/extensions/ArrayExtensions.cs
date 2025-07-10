


namespace AMToolkits.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ArrayExtensions
    {
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