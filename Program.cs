
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
    if(environment != "Development")
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
    if(cfg == null)
    {
        System.Console.WriteLine("Init Logger Error, Not Config");
        return;
    }

    // 0:
    var logger = Logger.LoggerFactory.CreateLogger(cfg.Name);
    logger.SetOutputFileName(cfg.File);
    logger.Log("Init Logger Completed");
    // 0 - 1:
    AMToolkits.Utility.ResourcesManager.NewInstance(args);
    // 0 - 2:
    AMToolkits.Utility.TableDataManager.NewInstance(args);
    AMToolkits.Utility.TableDataManager.GetTableData<Game.TAIPlayers>();


    // 1:
    var db_manager = Server.DatabaseManager.NewInstance(args, config);
    logger.Log("Init DatabaseManager Completed");

    // 2:
    var user_manager = Server.UserManager.NewInstance(args, config);
    logger.Log("Init UserManager Completed");

    // 2 - 1:
    var ai_manager = Server.AIPlayerManager.NewInstance(args, config);
    logger.Log("Init AIPlayerManager Completed");
    
    // 2 - 2:
    var room_manager = Server.RoomManager.NewInstance(args, config);
    logger.Log("Init RoomManager Completed");

    // 3:
    var match_manager = Server.GameMatchManager.NewInstance(args, config);
    logger.Log("Init GameMatchManager Completed");

    var proxy_service = Server.PlayFabService.NewInstance(args, config);

    // 
    var app = Server.ServerApplication.NewInstance(args, config);
    
    app.RegisterHandlersListner += user_manager.OnRegisterHandlers;
    app.RegisterHandlersListner += match_manager.OnRegisterHandlers;

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
    int result = await app.StartWorking();
    app.EndWorking();

} catch (Exception ex) {
    System.Console.WriteLine(ex);
}
