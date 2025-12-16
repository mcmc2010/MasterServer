
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServerApplication
    {
        #region WSServer
        private WSSServer? _wsserver = null;


        ////        
        public bool CreateWSServer()
        {
            if (_config == null)
            {
                System.Console.WriteLine("[Server] Config is NULL.");
                return false;
            }

            _wsserver = new WSSServer();
            _wsserver.Create(_arguments, _config);

            //
            _logger?.Log("[Server] Starting WSServer");
            return true;
        }
        #endregion
    }
}