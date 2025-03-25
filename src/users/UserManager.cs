

namespace Server
{
    public class UserManager : Utils.SingletonT<UserManager>, Utils.ISingleton
    {
        private string[]? _arguments = null;
        private ConfigEntry? _config = null;

        public UserManager()
        {

        }

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = paramters[0] as string[];

            var config = paramters[1] as ConfigEntry;
            if(config == null)
            {
                System.Console.WriteLine("[UserManager] Config is NULL.");
                return ;
            }
            _config = config;

            //
        }
    }
}