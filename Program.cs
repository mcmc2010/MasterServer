
//
using Logger;

try
{
    var logger = Logger.LoggerFactory.CreateLogger("main");


    //
    var config = Server.ServerConfig.LoadFromFile("settings.yaml");
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
