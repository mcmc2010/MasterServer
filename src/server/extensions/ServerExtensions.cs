
using AMToolkits;


namespace Server.Extensions
{

    public static class ServerManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        public static void InitLogger(this IServerManager manager, string name)
        {
            //
            manager.Logger = null;
            var cfg = manager.Config?.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == name);
            if (cfg != null)
            {
                if (!cfg.Enabled)
                {
                    manager.Logger = null;
                }
                else
                {
                    manager.Logger = Logger.LoggerFactory.CreateLogger(cfg.Name, cfg.IsConsole, cfg.IsFile);
                    manager.Logger.SetOutputFileName(cfg.File);
                }
            }
        }
    }
}