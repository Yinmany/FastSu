using System.Net;
using FastSu;
using FastSu.Server;
using HelloServer;
using NLog;

try
{
    // 添加一下消息所在的程序集
    AssemblyPartManager.Ins
        .AddPart(typeof(PingMsg).Assembly)
        .AddHotfixPart("HelloServer.Hotfix")
        .EnableWatch();

    var builder = FastSuHost.Create(args);
    builder.Services.AddFastSu(1, IPEndPoint.Parse("127.0.0.1:9000"));
    builder.Services.AddHostedService<HelloHostedService>();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    SLog.Error(ex, "Application terminated unexpectedly");
}
finally
{
    LogManager.Shutdown();
}

class HelloHostedService : IHostedService
{
    public HelloHostedService(IServiceProvider sp)
    {
        FacadeHelper.Inject(typeof(Hello), sp);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 指定id在此进程中创建服务
        long serviceId = Did.Make(1);
        ServiceId sid = ServiceMgr.Create(new StartupService(), serviceId);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}