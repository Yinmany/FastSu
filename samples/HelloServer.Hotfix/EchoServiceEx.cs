using Cysharp.Threading.Tasks;
using FastSu;
using FastSu.Core;

namespace HelloServer;

public class EchoServiceEx : AServiceEvent<EchoService>
{
    public override UniTask OnStart(EchoService self, CancellationToken cancellationToken)
    {
        return UniTask.CompletedTask;
    }

    public override UniTask OnStop(EchoService self, CancellationToken cancellationToken)
    {
        return UniTask.CompletedTask;
    }

    public override UniTask OnMessage(EchoService self, in Msg msg)
    {
        SLog.Info($"收到消息: {msg}");
        return UniTask.CompletedTask;
    }
}