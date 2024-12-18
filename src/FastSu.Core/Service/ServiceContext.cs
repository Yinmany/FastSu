using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace FastSu.Core;

public interface IServiceContext
{
    public long Id { get; }

    /// <summary>
    /// 服务实例
    /// </summary>
    public IService ServiceInstance { get; }

    /// <summary>
    /// 投递自定义回调
    /// </summary>
    /// <param name="d"></param>
    /// <param name="state"></param>
    void Post(SendOrPostCallback d, object? state);

    /// <summary>
    /// 接收到一个消息
    /// </summary>
    /// <param name="msg"></param>
    void Receive(in Msg msg);

    void Stop(CancellationToken cancellationToken);

    /// <summary>
    /// 异步停止
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    UniTask StopAsync(CancellationToken cancellationToken);
}

public sealed class ServiceContext : SingleThreadSynchronizationContext, IServiceContext
{
    public long Id { get; }

    public IService ServiceInstance { get; }

    private ServiceState _status; // 0启动中 1运行中 2停止中

    private readonly UniTaskSingleWaiterAutoResetEvent _workSignal;
    private readonly CancellationTokenSource _startCts;
    private readonly ConcurrentQueue<Msg> _messageQueue;

    private CancellationToken _stopForceToken;
    private UniTaskCompletionSource? _waitStopTcs;

    public ServiceContext(long id, IService ins, WorkThread? workThread = null) : base(workThread)
    {
        Id = id;
        ServiceInstance = ins;
        _workSignal = new UniTaskSingleWaiterAutoResetEvent(this);
        _startCts = new CancellationTokenSource();
        _messageQueue = new ConcurrentQueue<Msg>();
    }

    // private void ScheduleOperation(in Command command)
    // {
    //     lock (this)
    //     {
    //         _pendingOperations ??= new Queue<Command>();
    //         _pendingOperations.Enqueue(command);
    //     }
    //
    //     _workSignal.Signal();
    // }

    public void Receive(in Msg msg)
    {
        _messageQueue.Enqueue(msg);
        _workSignal.Signal();
    }

    public void Start()
    {
        lock (this)
        {
            if (_status != ServiceState.None)
                throw new InvalidOperationException("已经调用了Start.");
            _status = ServiceState.Starting;
        }

        RunMessageLoop().Forget();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken">stop超时时触发</param>
    public void Stop(CancellationToken cancellationToken)
    {
        // 先取消
        if (!_startCts.IsCancellationRequested)
        {
            _startCts.Cancel();
            _stopForceToken = cancellationToken;
            _workSignal.Signal();
        }
    }

    public UniTask StopAsync(CancellationToken cancellationToken)
    {
        _waitStopTcs = new UniTaskCompletionSource();
        Stop(cancellationToken);
        return _waitStopTcs.Task;
    }

    protected override void OnTick() => ServiceInstance.OnTick();

    private async UniTask InternalStartAsync()
    {
        try
        {
            // 执行开始
            // _startCts.CancelAfter(TimeSpan.FromMinutes(5)); // 用于start超时
            await ServiceInstance.OnStart(_startCts.Token);
        }
        catch (Exception e)
        {
            SLog.Error(e, $"Service启动异常: {Id}");
        }

        if (!_startCts.IsCancellationRequested)
        {
            lock (this)
            {
                _status = ServiceState.Running;
            }
        }
    }

    private async UniTask InternalStopAsync()
    {
        try
        {
            _status = ServiceState.Stopping;
            await ServiceInstance.OnStop(_stopForceToken);
        }
        catch (Exception e)
        {
            SLog.Error(e, $"Service停止异常: {Id}");
        }

        lock (this)
        {
            _status = ServiceState.Stopped;
        }

        _waitStopTcs?.TrySetResult();
    }

    private async UniTask RunMessageLoop()
    {
        await this.Yield();
        await InternalStartAsync();

        while (true)
        {
            try
            {
                // 当前没有在处理消息中，就执行操作命令
                if (!await ProcessOperationsAsync())
                    break;

                while (_messageQueue.TryDequeue(out Msg msg))
                {
                    try
                    {
                        await ServiceInstance.OnMessage(msg);
                    }
                    catch (Exception e)
                    {
                        // 消息异常
                        SLog.Error(e, $"Service处理消息异常: {Id} {msg}");
                    }
                }

                await _workSignal.WaitAsync();
            }
            catch (Exception e)
            {
                SLog.Error(e, "Error in service message loop");
            }
        }
    }

    private async UniTask<bool> ProcessOperationsAsync()
    {
        // 已经请求停止了
        if (_startCts.IsCancellationRequested)
        {
            await InternalStopAsync();
            return false;
        }

        // bool hasPendingOperations;
        // lock (this)
        // {
        //     hasPendingOperations = _pendingOperations is { Count: > 0 };
        // }
        //
        // if (hasPendingOperations)
        // {
        //     await ProcessOperationsAsync();
        // }

        return true;
    }
}