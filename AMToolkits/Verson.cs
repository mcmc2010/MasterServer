

namespace AMToolkits
{
    /// <summary>
    /// 
    /// </summary>
    public interface IVersion
    {
        static string? Build { get; }
        static string? Version { get; }
    }
}