
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

    public class ServiceConstants
    {
        /// <summary>
        /// 
        /// </summary>
        public const string KEY_ERROR = "error";
        public const string KEY_RESULT = "result";
        public const string KEY_DATA = "data";
        public const string KEY_DESC = "description";
        /// <summary>
        /// 
        /// </summary>
        public const string VALUE_SUCCESS = "success";
        public const string VALUE_ERROR = "error";
        public const string VALUE_NULL = "null";
        public const string VALUE_FAILED = "failed";
        public const string VALUE_EXPIRED = "expired";
        public const string VALUE_LIMIT = "limit";
        public const string VALUE_NOTFOUND = "not_found";
        public const string VALUE_NOTMATCH = "not_same";
        // 余额不足，或数量不够
        public const string VALUE_INSUFFICIENT = "insufficient";
        //
        public const string VALUE_PARAMETER_ERROR = "parameter_error";
    }
}