
using Logger;

namespace Server.Field
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FieldServer : AMToolkits.SingletonT<FieldServer>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static FieldServer? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;


        private readonly RoomManager _room_manager;

        public FieldServer()
        {
            _room_manager = new RoomManager();
        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[FieldServer] Config is NULL.");
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
            _logger?.Log("[FieldServer] Start Working");

            //
            float delay = 5.0f;
            //
            while (!ServerApplication.Instance.HasQuiting)
            {
                _room_manager.UpdateHeartbeat();
                await Task.Delay((int)(delay * 1000));
            }
            //
            return 0;
        }
    }
}