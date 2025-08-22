
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

namespace Server
{

    public interface IUserSession
    {
        string UID { get; } // 会话ID
        string UserID { get; } // 关联用户ID
        public Server.Services.IService? Service { get; }

        void InitUser(IUser user);

        public void BindService(Server.Services.IService service);
        public void FreeService();

        public Task BroadcastAsync(byte[] data, int index, int level);
    }

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
        [JsonPropertyName("network_uid")]
        public string network_id = ""; //网络ID

        

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string UID { get { return this.id; } }
        [JsonIgnore]
        public string UserID { get { return this.user_id; } }
        [JsonIgnore]
        public string NetworkID { get { return this.network_id; } }

        private IUser? _user = null;
        public UserBase? User { get { return (UserBase?)_user; } }
        private Server.Services.IService? _service = null;
        public Server.Services.IService? Service { get { return _service; } }


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

        public void BindService(Server.Services.IService service)
        {
            network_id = service.NetworkID;

            _service = service;
        }

        public void FreeService()
        {
            network_id = "";
            _service = null;
        }

        public async Task BroadcastAsync(byte[] data, int index, int level = 0)
        {
            if (_service == null)
            {
                return;
            }

            int result = await _service.BroadcastAsync(data, index, level);
        }
    }
}