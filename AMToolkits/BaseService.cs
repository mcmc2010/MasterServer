
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

    public class ServiceData
    {
        /// <summary>
        /// 
        /// </summary>
        public const string KEY_ERROR = "error";
        public const string KEY_RESULT = "result";
        public const string KEY_DATA = "data";
        /// <summary>
        /// 
        /// </summary>
        public const string VALUE_SUCCESS = "success";
        public const string VALUE_ERROR = "error";
        public const string VALUE_NULL = "null";
        public const string VALUE_EXPIRED = "expired";
        public const string VALUE_NOTFOUND = "not_found";
    }
}