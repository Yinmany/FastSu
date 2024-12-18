using Cysharp.Threading.Tasks;
using FastSu.Core;

namespace FastSu.Tests;

public class TestService : IService
{
    public async UniTask OnStart(CancellationToken cancellationToken)
    {
        SLog.Info("start...");
        // this.AddTimeout(TimeSpan.FromSeconds(1));

        cancellationToken.Register(f => SLog.Info("cancel..."), null);
        await Task.Delay(5000, cancellationToken);

        SLog.Info("start 2 ...");
    }

    public UniTask OnStop(CancellationToken cancellationToken)
    {
        SLog.Info("stop...");
        return UniTask.CompletedTask;
    }

    public void OnTick()
    {
    }

    public void OnTimer(ITimerNode timer)
    {
        SLog.Info($"onTimer: {timer}");
    }

    public async UniTask OnMessage(Msg msg)
    {
        SLog.Info($"收到消息: {msg}");
    }
}