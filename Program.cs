
//
using Logger;

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
        $"Development Mode: {(is_development ? "✔ Enabled" : "× Disabled")}\n"
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

    // 1:
    var user_manager = Server.UserManager.NewInstance(args, config);
    logger.Log("Init UserManager Completed");

    // 
    var app = Server.ServerApplication.NewInstance(args, config);
    
    app.RegisterHandlersListner += user_manager.OnRegisterHandlers;

    app.CreateHTTPServer();

    logger.Finish();

    //
    int result = await app.StartWorking();
    app.EndWorking();

} catch (Exception ex) {
    System.Console.WriteLine(ex);
}
