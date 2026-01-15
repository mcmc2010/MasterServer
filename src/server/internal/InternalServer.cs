
using Logger;
using Microsoft.AspNetCore.Builder;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class InternalService : AMToolkits.SingletonT<InternalService>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static InternalService? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public InternalService()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[InternalService] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }

        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            args.app?.Map("api/internal/user/wallet/data", HandleUserWalletData);
            args.app?.Map("api/internal/user/wallet/update", HandleUserWalletUpdate);

            args.app?.Map("api/internal/user/inventory/add", HandleAddUserInventoryItems);
            args.app?.Map("api/internal/user/inventory/consumable", HandleConsumableUserInventoryItems);

            //
            args.app?.Map("api/internal/game/pvp/completed", HandleGamePVPCompleted);

            // 下面API为代理服务
            args.app?.Map("api/v1/sandbox/payment", HandleOpenAPIPaymentResult);
            args.app?.Map("api/v1/openapp/payment", HandleOpenAPIPaymentResult);

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
            float delay = 1.0f;

            await Task.Delay((int)(delay * 1000));

            //var result_data = new NUserInventoryItemsResult();
            //await this._AddUserInventoryItems("157248358170", "C982468BE34D7C0B", "7001|100", result_data, "mail");
            return 0;
        }
    }
}