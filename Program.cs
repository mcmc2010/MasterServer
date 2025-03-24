using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


try
{
    //
    var config = Server.ServerConfig.LoadConfigFromFile("settings.yaml");

    //
    var app = Server.ServerApplication.NewInstance(config);


} catch (Exception ex) {
    System.Console.WriteLine(ex);
}



//
var builder = WebApplication.CreateBuilder(args);

// 添加服务配置
// 配置 Kestrel HTTPS
builder.WebHost.ConfigureKestrel(serverOptions => {
    serverOptions.Listen(IPAddress.Any, 5000);   // 监听 HTTP 5000 端口
    serverOptions.Listen(IPAddress.Any, 5443, listenOptions =>
    {
        listenOptions.UseHttps("https.pfx", ""); // 配置 HTTPS
    });
});

//
// 添加文件日志提供程序，并设置日志路径和级别
//builder.Logging.AddFile("logs/main.log", minimumLevel: LogLevel.Information);
builder.Logging.SetMinimumLevel(LogLevel.Information);
// 注册自定义文件日志提供程序
builder.Logging.AddProvider(new FileLoggerProvider("./logs/main.log"));

//
var app = builder.Build();

// 基础端点
app.MapGet("/", () => Results.Json(new 
{ 
    Status = "Ok", 
    Timestamp = DateTime.UtcNow 
}));

//
app.Run();