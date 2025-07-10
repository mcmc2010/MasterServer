
using Logger;
using Microsoft.AspNetCore.Builder;

namespace Server
{
    public enum UserGender
    {
        Female = 0,
        Male = 1
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PrivilegeLevel
    {
        None = 0,   //Guest
        Normal = 1, //认证或绑定后的用户
        Master = 7,
    }
    
    /// <summary>
    /// 
    /// </summary>
    public interface IUser
    {

    }

    public interface IUserSession
    {
        public void BindService(Server.Services.IService service);
        public void FreeService();

        public Task BroadcastAsync(byte[] data, int index, int level);
    }

    [System.Serializable]
    public class UserBase : IUser
    {
        public string ID = "";
        public string ClientID = "";
        public string AccessToken = "";
        public string Passphrase = "";
        public DateTime Time = DateTime.Now;
        public int PrivilegeLevel = 0;
    }

    [System.Serializable]
    public class UserSession : UserBase, IUserSession
    {
        private string _network_id = "";
        
        private Server.Services.IService? _service = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public UserSession(UserBase user)
        {
            //
            this.ID = user.ID;
            this.ClientID = user.ClientID;
            this.AccessToken = user.AccessToken;
            this.Passphrase = user.Passphrase;
            this.Time = user.Time;

            //
            this.PrivilegeLevel = user.PrivilegeLevel;
        }

        public void BindService(Server.Services.IService service)
        {
            _network_id = service.NetworkID;
            _service = service;
        }

        public void FreeService()
        {
            _network_id = "";
            _service = null;
        }

        public async Task BroadcastAsync(byte[] data, int index, int level = 0)
        {
            if(_service == null) {
                return;
            }

            int result = await _service.BroadcastAsync(data, index, level);
        }
    }

    public partial class UserManager : AMToolkits.SingletonT<UserManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static UserManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private object _users_lock = new object();
        private Dictionary<string, IUser> _users = new Dictionary<string, IUser>();

        public UserManager()
        {

        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[UserManager] Config is NULL.");
                return ;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;

            //
            _users.Clear();
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[UserManager] Register Handlers");

            //
            args.app?.Map("api/user/auth", HandleUserAuth);

        }

        public bool AddUser(UserBase user)
        {
            user.ID = user.ID.Trim();
            if(string.IsNullOrEmpty(user.ID))
            {
                return false;
            }

            user.Time = DateTime.Now;

            UserSession session = new UserSession(user);
            lock (_users_lock)
            {
                _users[user.ID] = session;
            }
            return true;
        }

        public UserBase? GetUser(string id)
        {
            id = id.Trim();

            IUser? user;
            lock(_users_lock) {
                _users.TryGetValue(id, out user);
            }
            return (UserBase?)user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public UserSession? GetAuthenticationSession(string id, string token)
        {
            if(id.Length == 0 || token.Length == 0)
            {
                return null;
            }
            
            var user = this.GetUser(id);
            if(user == null) { 
                return null;
            }

            if(user.AccessToken != token.ToUpper())
            {
                return null;
            }
            return (UserSession?)user;
        }

        /// <summary>
        /// 广播给所有用户
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task<int> BroadcastAsync(byte[] data, int index, int level = 0)
        {
            if(data.Length == 0)
            {
                return 0;
            }

            int count = 0;
            //
            foreach (var user in _users)
            {
                var session = user.Value as UserSession;
                if(session == null) { continue; }

                session.BroadcastAsync(data, index, level);
                count ++;
            }

            return count;
        }
    }
}