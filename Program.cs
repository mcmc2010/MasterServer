
//
using Logger;

try
{
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
