

try
{
    //
    var config = Server.ServerConfig.LoadFromFile("settings.yaml");

    //
    var app = Server.ServerApplication.NewInstance(args, config);
    app.CreateHTTPServer();

    //
    int result = await app.StartWorking();
    app.EndWorking();

} catch (Exception ex) {
    System.Console.WriteLine(ex);
}
