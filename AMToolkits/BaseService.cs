
namespace AMToolkits
{
    public enum ServiceStatus
    {
        None = -1,
        Initialized = 0,
        Ready = 1,
        /// <summary>
        /// 配置错误或者是连接错误
        /// </summary>
        Refuse = -10
    }
}