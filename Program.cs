


//
CancellationTokenSource cts = new CancellationTokenSource();
void OnProcessExit(object? sender, EventArgs e)
{
    cts.Cancel();
}

//
void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true; // 阻止直接退出
    cts.Cancel();
}


try
{
    //
    var config = Server.ServerConfig.LoadFromFile("settings.yaml");

    //
    var app = Server.ServerApplication.NewInstance(config);
    app.CreateHTTPServer();

    //
    app.StartWorking();
    
    
    // 注册终止信号处理
    Console.CancelKeyPress += OnCancelKeyPress;
    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(1000, cts.Token);
    }

    app.EndWorking();

} catch (Exception ex) {
    System.Console.WriteLine(ex);
}
