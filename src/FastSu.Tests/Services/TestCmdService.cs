using System.Diagnostics;
using Cysharp.Threading.Tasks;

namespace FastSu.Core.Tests;

public class TestCmdService : IService
{
    private int num = 0;
    private Stopwatch s = Stopwatch.StartNew();
    private List<long> _list = new List<long>();

    public UniTask OnStart(CancellationToken cancellationToken)
    {
        return UniTask.CompletedTask;
    }

    public UniTask OnStop(CancellationToken cancellationToken)
    {
        Console.WriteLine($"25次: 100w 平均耗时={_list.Average()}");
        return UniTask.CompletedTask;
    }

    public void OnTimer(ITimerNode timer)
    {
    }

    public async UniTask OnMessage(Msg msg)
    {
        // 复制两次
        Msg msg1 = msg;
        Msg msg2 = msg;

        ++num;
        if (num % 1000000 == 0)
        {
            Console.WriteLine($"100w耗时: {s.ElapsedMilliseconds}");
            _list.Add(s.ElapsedMilliseconds);
            s.Restart();
        }
    }

    public void OnTick()
    {
    }
}