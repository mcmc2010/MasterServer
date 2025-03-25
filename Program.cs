
//
using Logger;

try
{
    //
    var config = Server.ServerConfig.LoadFromFile("settings.yaml");
    var logger = Logger.LoggerFactory.CreateLogger("main");
    logger.Log("Init Logger Completed");

    //
    Server.UserManager.NewInstance(args, config);

    //
    var app = Server.ServerApplication.NewInstance(args, config);
    app.CreateHTTPServer();

    logger.Finish();

    //
    int result = await app.StartWorking();
    app.EndWorking();

} catch (Exception ex) {
    System.Console.WriteLine(ex);
}
