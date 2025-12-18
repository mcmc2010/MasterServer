
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public interface IUserSession
    {
        string UID { get; } // 会话ID
        string UserID { get; } // 关联用户ID

        void InitUser(IUser user);
    }

    /// <summary>
    /// 用户会话，惰性会话不具有实时性
    /// </summary>
    [System.Serializable]
    public class UserSession : IUserSession
    {
        /// <summary>
        /// 序列化数据
        /// </summary>
        [JsonPropertyName("uid")]
        public string id = ""; //会话ID
        [JsonPropertyName("user_uid")]
        public string user_id = ""; //会话ID

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string UID { get { return this.id; } }
        [JsonIgnore]
        public string UserID { get { return this.user_id; } }

        private IUser? _user = null;
        public UserBase? User { get { return (UserBase?)_user; } }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public UserSession()
        {
            this.id = AMToolkits.Utility.Guid.GeneratorID10();
        }

        public void InitUser(IUser user)
        {
            this.user_id = user.UID;
            this._user = user;
        }
    }
}