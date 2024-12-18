using Cysharp.Threading.Tasks;

namespace FastSu.Core;

public interface IServiceEvent
{
    public int Id { get; }
}

public abstract class AServiceEvent<T> : IServiceEvent where T : IService
{
    public int Id => TypeId.Get(typeof(T));
    public abstract UniTask OnStart(T self, CancellationToken cancellationToken);
    public abstract UniTask OnStop(T self, CancellationToken cancellationToken);

    public virtual void OnTick(T self)
    {
    }

    public virtual UniTask OnMessage(T self, in Msg msg)
    {
        return UniTask.CompletedTask;
    }

    public virtual void OnTimer(T self, ITimerNode timer)
    {
    }
}

public readonly struct ServiceEvent<T> where T : IService
{
    private readonly int _id = TypeId.Get(typeof(T));
    private readonly T _service;

    public ServiceEvent(T service)
    {
        _service = service;
    }

    public UniTask Start(CancellationToken cancellationToken)
    {
        var e = ServiceEventCenter.Ins.Get(this._id);
        if (e == null)
            return UniTask.CompletedTask;
        return ((AServiceEvent<T>)e).OnStart(_service, cancellationToken);
    }

    public UniTask Stop(CancellationToken cancellationToken)
    {
        var e = ServiceEventCenter.Ins.Get(this._id);
        if (e == null)
            return UniTask.CompletedTask;
        return ((AServiceEvent<T>)e).OnStop(_service, cancellationToken);
    }

    public void Tick()
    {
        var e = ServiceEventCenter.Ins.Get(this._id);
        if (e == null)
            return;
        ((AServiceEvent<T>)e).OnTick(_service);
    }

    public UniTask Message(in Msg msg)
    {
        var e = ServiceEventCenter.Ins.Get(this._id);
        if (e == null)
            return UniTask.CompletedTask;
        return ((AServiceEvent<T>)e).OnMessage(_service, msg);
    }

    public void Timer(ITimerNode timer)
    {
        var e = ServiceEventCenter.Ins.Get(this._id);
        if (e == null)
            return;
        ((AServiceEvent<T>)e).OnTimer(_service, timer);
    }
}

/// <summary>
/// 用于把服务的事件派发到热更层
/// </summary>
internal class ServiceEventCenter : Singleton<ServiceEventCenter>, IAssemblyPostProcess
{
    private Dictionary<int, IServiceEvent> _events;
    private Dictionary<int, IServiceEvent>? _tmp;

    void IAssemblyPostProcess.Begin()
    {
        _tmp = new Dictionary<int, IServiceEvent>();
    }

    void IAssemblyPostProcess.Process(Type type, bool isHotfix)
    {
        if (!type.IsAssignableTo(typeof(IServiceEvent)))
            return;

        IServiceEvent iServiceEvent = (IServiceEvent)Activator.CreateInstance(type)!;
        _tmp.Add(iServiceEvent.Id, iServiceEvent);
    }

    void IAssemblyPostProcess.End()
    {
        Interlocked.Exchange(ref _events, _tmp);
        _tmp = null;
    }

    public IServiceEvent? Get(int id)
    {
        _events.TryGetValue(id, out var value);
        return value;
    }
}