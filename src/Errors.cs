
namespace Server
{
    public class ErrorMessage
    {
        public const string ERROR = "Error";
        public const string UNKNOW = "Unknow";
        public const string NOT_FOUND = "Not Found";
        public const string TOO_MANY_REQUESTS = "Too Many Requests";
        
        /// <summary>
        /// 
        /// </summary>
        public const string NotAllowAccess_Unauthorized_NotKey = "Please contact the developer, unauthorized access";
        public const string NotAllowAccess_Unauthorized_NotLogin = "Please login your account, unauthorized access";
        public const string NotAllowAccess_Unauthorized_TooMany = "Too many requests, unauthorized access";
    }
}