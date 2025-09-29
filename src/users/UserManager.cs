
//
using AMToolkits.Extensions;
using Logger;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;


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

        private Dictionary<string, Server.Services.IService?> _service_list = new Dictionary<string, Services.IService?>();

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

            //
            _service_list.Clear();

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
            args.app?.Map("api/user/profile/data", HandleUserProfile);
            args.app?.Map("api/user/profile/update", HandleUserUpdateProfile);
            args.app?.MapPost("api/user/name/change", HandleUserChangeName);
            //
            args.app?.Map("api/user/inventory/list", HandleGetUserInventoryItems);
            args.app?.MapPost("api/user/inventory/using", HandleUsingUserInventoryItem);
            args.app?.MapPost("api/user/inventory/upgrade", HandleUpgradeUserInventoryItem);
            // Rank
            args.app?.MapPost("api/user/rank/data", HandleGetUserRank);
            // Game Events
            args.app?.Map("api/user/game/events/list", HandleGetUserGameEvents);
            // Game Effects
            args.app?.Map("api/user/game/effects/list", HandleGetUserGameEffects);
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

        /// <summary>
        /// Key是用户ID
        /// </summary>
        /// <param name="session"></param>
        public void InitSession(IUserSession session)
        {

#if USING_REDIS
            AMToolkits.Redis.RedisManager.Instance.SetNodeKeyValue(KEY_SESSIONS, session.UserID, session);
#endif
            _service_list.Set(session.UserID, session.Service);
        }

        public void FreeSession(IUserSession session)
        {
#if USING_REDIS
            AMToolkits.Redis.RedisManager.Instance.DeleteNodeKeyValue(KEY_SESSIONS, session.UserID);
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
            // 分批次广播消息
            int batch = 0;
            int total = await AMToolkits.Redis.RedisManager.Instance.GetNodeKeyValuesT<UserSession>(KEY_SESSIONS,
                (sessions) =>
                {
                    if (sessions.Count > 0)
                    {
                        foreach (var session in sessions)
                        {
                            if (session == null) { continue; }

                            Server.Services.IService? service = null;
                            _service_list.TryGetValue(session.UserID, out service);
                            if (service == null) { continue; }
                            session.BindService(service);

                            session?.BroadcastAsync(data, index, level);

                            count++;
                        }
                    }
                    batch++;
                    return true;
                });
            

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