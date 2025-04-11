using AMToolkits.Utility;
using Logger;
using Microsoft.AspNetCore.Builder;

namespace Server
{
    [System.Serializable]
    public class UserBase
    {
        public string ID = "";
        public string ClientID = "";
        public string AccessToken = "";
        public string Passphrase = "";
        public DateTime Time = DateTime.Now;
    }

    public partial class UserManager : SingletonT<UserManager>, ISingleton
    {
        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private object _users_lock = new object();
        private Dictionary<string, UserBase> _users = new Dictionary<string, UserBase>();

        public UserManager()
        {

        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

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
            lock(_users_lock) {
                _users[user.ID] = user;
            }
            return true;
        }

        public UserBase? GetUser(string id)
        {
            id = id.Trim();

            UserBase? user;
            lock(_users_lock) {
                _users.TryGetValue(id, out user);
            }
            return user;
        }

    }
}