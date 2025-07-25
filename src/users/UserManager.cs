
using AMToolkits.Extensions;
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
        void InitUser(IUser user);

        public void BindService(Server.Services.IService service);
        public void FreeService();

        public Task BroadcastAsync(byte[] data, int index, int level);
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserBase : IUser
    {
        public string ID = "";
        public string ClientID = "";
        public string AccessToken = "";
        public string Passphrase = "";
        /// <summary>
        /// 认证的自定义ID
        /// </summary>
        public string CustomID = "";
        /// <summary>
        /// 
        /// </summary>
        public DateTime Time = DateTime.Now;
        /// <summary>
        /// 
        /// </summary>
        public int PrivilegeLevel = 0;
    }

    [System.Serializable]
    public class UserSession : IUserSession
    {
        private IUser? _user = null;
        public UserBase? User { get { return (UserBase?)_user; } }
        
        private string _network_id = "";
        
        private Server.Services.IService? _service = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public UserSession()
        {
        }

        public void InitUser(IUser user)
        {
            this._user = user;
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

        private object _async_lock = new object();
        private Dictionary<string, IUser> _user_list = new Dictionary<string, IUser>();
        private Dictionary<string, IUserSession> _session_list = new Dictionary<string, IUserSession>();

        public UserManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[UserManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;

            //
            _user_list.Clear();
            _session_list.Clear();
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[UserManager] Register Handlers");

            //
            args.app?.Map("api/user/auth", HandleUserAuth);
            //
            args.app?.Map("api/user/inventory/list", HandleGetUserInventoryItems);
        }

        public TU AllocT<TU>() where TU : UserBase, new()
        {
            var user = new TU();
            return user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool AddUser(UserBase user)
        {
            user.ID = user.ID.Trim();
            if (string.IsNullOrEmpty(user.ID))
            {
                return false;
            }

            user.Time = DateTime.Now;

            //
            lock (_async_lock)
            {
                _user_list[user.ID] = user;
            }
            return true;
        }

        /// <summary>
        /// 获取用户基础类
        /// </summary>
        /// <typeparam name="TU"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TU? GetUserT<TU>(string id) where TU : UserBase
        {
            id = id.Trim();
            if (id.IsNullOrWhiteSpace()) { return default(TU); }

            IUser? user;
            lock (_async_lock)
            {
                _user_list.TryGetValue(id, out user);
            }
            return (TU?)user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public UserSession? RequireAuthenticationSession(string id, string token)
        {
            if(id.Length == 0 || token.Length == 0)
            {
                return null;
            }
            
            var user = this.GetUserT<UserBase>(id);
            if(user == null) { 
                return null;
            }

            if(user.AccessToken != token.ToUpper())
            {
                return null;
            }

            IUserSession? session;
            lock (_async_lock)
            {
                _session_list.TryGetValue(id, out session);
                if (session == null)
                {
                    session = new UserSession();
                    session.InitUser(user);
                    _session_list.Add(user.ID, session);
                }
            }
            return (UserSession?)session;
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
            foreach (var v in _session_list)
            {
                var session = v.Value;
                if (session == null) { continue; }

                session?.BroadcastAsync(data, index, level);
                count ++;
            }

            return count;
        }
    }
}