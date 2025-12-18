
using Logger;


namespace Server.World
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WorldServer : AMToolkits.SingletonT<WorldServer>, AMToolkits.ISingleton
    {

        [AMToolkits.AutoInitInstance]
        protected static WorldServer? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paramters"></param>
        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[WorldServer] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }

#pragma warning disable CS4014
        public int StartWorking()
        {
            //
            this.ProcessWorking();
            return 0;
        }
#pragma warning restore CS4014

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<int> ProcessWorking()
        {
            _logger?.Log("[WorldServer] Start Working");

            //
            float delay = 5.0f;
            //
            while (!ServerApplication.Instance.HasQuiting)
            {
                await Task.Delay((int)(delay * 1000));
            }
            //
            return 0;
        }
    }
}