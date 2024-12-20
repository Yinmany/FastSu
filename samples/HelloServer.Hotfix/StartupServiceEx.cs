using System.Net;
using Cysharp.Threading.Tasks;
using FastSu;

namespace HelloServer;

public class StartupServiceEx : AServiceEvent<StartupService>
{
    public override UniTask OnStart(StartupService self, CancellationToken cancellationToken)
    {
        SLog.Info($"start {self.GetId()}");

        Hello.Rpc.Register(1, IPEndPoint.Parse("127.0.0.1:9000"));

        // 添加一个每隔5秒执行一下的定时器
        self.PingTimer = self.AddInterval(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), 1);

        // 创建echo 服务
        self.EchoId = ServiceMgr.Create(new EchoService());

        return UniTask.CompletedTask;
    }

    public override UniTask OnStop(StartupService self, CancellationToken cancellationToken)
    {
        SLog.Info($"stop {self.GetId()}");
        self.PingTimer?.Dispose();
        return UniTask.CompletedTask;
    }

    public override void OnTimer(StartupService self, ITimerNode timer)
    {
        if (timer.Type == 1)
        {
            // 向echo 服务发送消息(本地消息)
            self.EchoId.Send(new PingMsg());

            // 通过远程发送
            Hello.Rpc.Send(self.EchoId.Id, new PingMsg(), 1);
        }
    }
}