
//
using Logger;
using Server;

bool is_development = true;

try
{
    // 获取操作系统信息
    var os_description = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    var os_architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;

    // 判断是否为开发模式
    var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
               ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
               ?? "Production";
#if DEBUG
    environment = "Development";
#endif
    if (environment != "Development")
    {
        is_development = false;
    }

    // 输出格式化信息
    System.Console.WriteLine(
        $"[System Environment]\n" +
        $"OS: {os_description}\n" +
        $"Architecture: {os_architecture}\n" +
        $"Development Mode: {(is_development ? "Enabled" : "Disabled")}\n"
    );

    //
    var config = Server.ServerConfigLoader.LoadFromFile("settings.yaml");
    var cfg = config.Logging.FirstOrDefault(v => v.Name.Trim().ToLower() == "main");
    if (cfg == null)
    {
        System.Console.WriteLine("Init Logger Error, Not Config");
        return;
    }

    var settings = Server.GameSettingsInstance.LoadFromFile("data/game_settings.txt");

    // 0:
    var logger = Logger.LoggerFactory.CreateLogger(cfg.Name);
    logger.SetOutputFileName(cfg.File);
    logger.Log("Init Logger Completed");
    logger.Log($"Server Index - {config.ServerIndex}");

    // 0 - 1:
    AMToolkits.Utility.ResourcesManager.NewInstance(args);
    // 0 - 2:
    AMToolkits.Utility.TableDataManager.NewInstance(args);
    AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
    AMToolkits.Utility.TableDataManager.GetTableData<Game.TAIPlayers>();
    AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
    AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();

    logger.Log("Init TableData Completed");

    // 1:
    var db_manager = Server.DatabaseManager.NewInstance(args, config);
    logger.Log("Init DatabaseManager Completed");

#if USING_REDIS
    // 1-1:
    Logger.LoggerEntry? redis_logger = null;
    var redis_logger_config = config.Logging.FirstOrDefault(v => v.Name == "redis");
    if (redis_logger_config != null)
    {
        redis_logger = Logger.LoggerFactory.CreateLogger(redis_logger_config.Name, redis_logger_config.IsConsole, redis_logger_config.IsFile);
        redis_logger.SetOutputFileName(redis_logger_config.File);
    }
    AMToolkits.Redis.RedisManager.NewInstance(args, new AMToolkits.Redis.RedisOptions()
    {
        Name = config.Redis.Name,
        Address = config.Redis.Address,
        Port = config.Redis.Port,
        UserName = config.Redis.UserName,
        Password = config.Redis.Password,
        HasSSL = config.Redis.HasSSL,
        SSLCertificates = config.Redis.SSLCertificates,
        SSLKey = config.Redis.SSLKey,
    },
    redis_logger ?? logger);
    logger.Log("Init RedisManager Completed");

#endif

    // 2 - 0:
    var user_manager = Server.UserManager.NewInstance(args, config);
    logger.Log("Init UserManager Completed");

    // 2 - 1:
    var market_manager = Server.MarketManager.NewInstance(args, config);
    var cashshop_manager = Server.CashShopManager.NewInstance(args, config);
    var gameevents_manager = Server.GameEventsManager.NewInstance(args, config);

    // 3 - 0:
    var ai_manager = Server.AIPlayerManager.NewInstance(args, config);
    logger.Log("Init AIPlayerManager Completed");

    // 4:
    var room_manager = Server.RoomManager.NewInstance(args, config);
    logger.Log("Init RoomManager Completed");

    // 5:
    var match_manager = Server.GameMatchManager.NewInstance(args, config);
    logger.Log("Init GameMatchManager Completed");

    // 6 - 1:
    var payment_manageer = Server.PaymentManager.NewInstance(args, config);
    // 6 - 2:
    var proxy_service = Server.PlayFabService.NewInstance(args, config);

    // 7:
    var internal_service = Server.InternalService.NewInstance(args, config);

    // 8:
    var leaderboard_manager = Server.LeaderboardManager.NewInstance(args, config);

    
    // 
    var app = Server.ServerApplication.NewInstance(args, config);

    app.RegisterHandlersListner += user_manager.OnRegisterHandlers;
    app.RegisterHandlersListner += market_manager.OnRegisterHandlers;
    app.RegisterHandlersListner += cashshop_manager.OnRegisterHandlers;
    app.RegisterHandlersListner += gameevents_manager.OnRegisterHandlers;
    
    app.RegisterHandlersListner += match_manager.OnRegisterHandlers;
    app.RegisterHandlersListner += internal_service.OnRegisterHandlers;

    app.RegisterHandlersListner += leaderboard_manager.OnRegisterHandlers;

    app.RegisterHandlersListner += payment_manageer.OnRegisterHandlers;
    
    app.CreateHTTPServer();
    app.CreateWSServer();

    logger.Finish();

    //
    room_manager.StartWorking();
    Thread.Sleep(100);
    match_manager.StartWorking();

    //
    proxy_service.StartWorking();
    // 
    payment_manageer.StartWorking();

    //
    int result = await app.StartWorking();
    app.EndWorking();

    AMToolkits.Redis.RedisManager.Instance.Dispose();

}
catch (Exception ex)
{
    System.Console.WriteLine(ex);
}
