using FastSu.Core;
using NLog;
using NLog.LayoutRenderers;

namespace FastSu.Tests;

public class ServiceTests
{
    WorkThread workThread = new WorkThread();

    [SetUp]
    public void Setup()
    {
        LogManager.Setup()
            .SetupExtensions(f => { f.RegisterLayoutRenderer<ColoredConsoleLayout>(); })
            .LoadConfigurationFromFile("NLog.config", false);

        Did.Init(1);
    }

    [Test]
    public async Task TestCall()
    {
        SLog.Info("创建服务...");
        TestService test = new TestService();
        ServiceId testId = ServiceMgr.Create(test, 1, workThread: workThread);

        SLog.Info("服务创建后...");

        testId.Send(new TestMsg());
        // IResponse ack = await testId.Call(new TestReq());
        // SLog.Info($"回应:{ack}");

        await Task.Delay(3000);
        await ServiceMgr.ShutdownAsync();
    }
}