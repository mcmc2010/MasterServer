
//
using AMToolkits.Extensions;
using Logger;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;
using MySqlX.XDevAPI;


#if USING_REDIS
using AMToolkits.Redis;   
#endif

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
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
        string UID { get; }
    }

    public interface IUserSession
    {
        string UID { get; }

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

        //
        public string UID { get { return this.ID; } }
    }

    [System.Serializable]
    public class UserSession : IUserSession
    {
        public string ID = ""; //会话ID
        public string UID { get { return this.ID; } }

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
            this.ID = AMToolkits.Utility.Guid.GeneratorID10();
        }

        public void InitUser(IUser user)
        {
            this.ID = user.UID;
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
            if (_service == null)
            {
                return;
            }

            int result = await _service.BroadcastAsync(data, index, level);
        }
    }

    
    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager : AMToolkits.SingletonT<UserManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static UserManager? _instance;

        public const string KEY_USERS = "users";
        public const string KEY_SESSIONS = "sessions";

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

#if USING_REDIS
            RedisManager.Instance.SetNodeKey(KEY_USERS);
            RedisManager.Instance.SetNodeKey(KEY_SESSIONS);
#endif
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log("[UserManager] Register Handlers");

            //
            args.app?.Map("api/user/auth", HandleUserAuth);
            args.app?.Map("api/user/profile", HandleUserProfile);
            //
            args.app?.Map("api/user/inventory/list", HandleGetUserInventoryItems);
            args.app?.MapPost("api/user/inventory/using", HandleUsingUserInventoryItem);
            args.app?.MapPost("api/user/inventory/upgrade", HandleUpgradeUserInventoryItem);
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
#if USING_REDIS
            AMToolkits.Redis.RedisManager.Instance.SetNodeKeyValue(KEY_USERS, user.ID, user);
#else
            lock (_async_lock)
            {
                _user_list[user.ID] = user;
            }
#endif
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
#if USING_REDIS
            user = AMToolkits.Redis.RedisManager.Instance.GetNodeKeyValueT<TU>(KEY_USERS, id);
#else
            lock (_async_lock)
            {
                _user_list.TryGetValue(id, out user);
            }
#endif
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
            if (id.Length == 0 || token.Length == 0)
            {
                return null;
            }

            var user = this.GetUserT<UserBase>(id);
            if (user == null)
            {
                return null;
            }

            if (user.AccessToken != token.ToUpper())
            {
                return null;
            }

            IUserSession? session;
#if USING_REDIS
            session = AMToolkits.Redis.RedisManager.Instance.GetNodeKeyValueT<UserSession>(KEY_SESSIONS, id);
            if (session == null)
            {
                session = new UserSession();
            }

            session.InitUser(user);
            AMToolkits.Redis.RedisManager.Instance.SetNodeKeyValue(KEY_SESSIONS, id, session);
#else
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
#endif
            return (UserSession?)session;
        }

        public void FreeSession(IUserSession session)
        {
#if USING_REDIS
            AMToolkits.Redis.RedisManager.Instance.DeleteNodeKeyValue(KEY_SESSIONS, session.UID);
#else
            lock (_async_lock)
            {
                _session_list.TryGetValue(session.UID, out session);
                if (session != null)
                {
                    _session_list.(session.UID);
                }
            }
#endif
        }

        /// <summary>
        /// 广播给所有用户
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task<int> BroadcastAsync(byte[] data, int index, int level = 0)
        {
            if (data.Length == 0)
            {
                return 0;
            }

            int count = 0;
#if USING_REDIS
            int batch = 0;
            //分批次广播消息
            List<UserSession?> sessions = await AMToolkits.Redis.RedisManager.Instance.GetNodeKeyValuesT<UserSession>(KEY_SESSIONS);
            do
            {
                if (sessions.Count > 0)
                {
                    foreach (var session in sessions)
                    {
                        session?.BroadcastAsync(data, index, level);
                        count++;
                    }
                }

                batch++;
                sessions = await AMToolkits.Redis.RedisManager.Instance.GetNodeKeyValuesT<UserSession>(KEY_SESSIONS, count);
            } while (sessions.Count > 0);
#else
            //
            foreach (var v in _session_list)
            {
                var session = v.Value;
                if (session == null) { continue; }

                session?.BroadcastAsync(data, index, level);
                count++;
            }
#endif
                return count;
        }
    }
}