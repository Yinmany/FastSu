using Cysharp.Threading.Tasks;

namespace FastSu;

/// <summary>
/// 把事件全部转发到逻辑层
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AService<T> : IService where T : AService<T>
{
    private readonly ServiceEvent<T> _serviceEvent;

    protected AService()
    {
        _serviceEvent = new ServiceEvent<T>((T)this);
    }

    public UniTask OnStart(CancellationToken cancellationToken)
    {
        return _serviceEvent.Start(cancellationToken);
    }

    public UniTask OnStop(CancellationToken cancellationToken)
    {
        return _serviceEvent.Stop(cancellationToken);
    }

    public void OnTick()
    {
        _serviceEvent.Tick();
    }

    public void OnTimer(ITimerNode timer)
    {
        _serviceEvent.Timer(timer);
    }

    public UniTask OnMessage(Msg msg)
    {
        return _serviceEvent.Message(msg);
    }
}