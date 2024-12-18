using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace FastSu.Core;

/// <summary>
/// 服务管理器
/// </summary>
public static class ServiceMgr
{
    private static readonly Dictionary<long, IServiceContext> Services = new();
    private static readonly ReaderWriterLockSlim RwLock = new();

    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="service"></param>
    /// <param name="id">可指定一个id;默认使用Did创建</param>
    /// <param name="workThread">可指定工作线程;默认使用线程池</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">id重复</exception>
    public static ServiceId Create(IService service, long? id = null, WorkThread? workThread = null)
    {
        long serviceId = id ?? Did.Next();
        ServiceContext serviceContext;
        RwLock.EnterWriteLock();
        try
        {
            if (Services.ContainsKey(serviceId))
                throw new ArgumentException($"id重复: {serviceId}");
            serviceContext = new ServiceContext(serviceId, service, workThread);
            Services.Add(serviceId, serviceContext);
        }
        finally
        {
            RwLock.ExitWriteLock();
        }

        serviceContext.Start();
        return serviceId;
    }

    /// <summary>
    /// 销毁服务
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken">用于停止超时的取消token</param>
    public static void Destroy(ServiceId id, CancellationToken cancellationToken = default)
    {
        RwLock.EnterWriteLock();
        IServiceContext? service;
        try
        {
            if (!Services.Remove(id.Id, out service))
                return;
        }
        finally
        {
            RwLock.ExitWriteLock();
        }

        service.Stop(cancellationToken);
    }

    /// <summary>
    /// 停机操作(并行停止所有)
    /// </summary>
    /// <param name="forceStopToken">需要强制停止的Token</param>
    /// <returns></returns>
    public static UniTask ShutdownAsync(CancellationToken forceStopToken = default)
    {
        RwLock.EnterWriteLock();
        try
        {
            List<UniTask> tasks = new List<UniTask>();
            foreach (var ctx in Services.Values)
            {
                tasks.Add(ctx.StopAsync(forceStopToken));
            }

            Services.Clear();
            return UniTask.WhenAll(tasks);
        }
        finally
        {
            RwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 投递消息
    /// </summary>
    /// <param name="id"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    internal static bool Post(long id, in Msg msg)
    {
        RwLock.EnterReadLock();
        try
        {
            if (!Services.TryGetValue(id, out IServiceContext? ctx))
                return false;
            ctx.Receive(msg);
            return true;
        }
        finally
        {
            RwLock.ExitReadLock();
        }
    }

    #region 消息相关

    private static readonly ConcurrentDictionary<int, ResponseTcs> Callbacks = new();
    private static int _rpcIdGen = 0;

    /// <summary>
    /// 发送一个消息
    /// </summary>
    /// <param name="id"></param>
    /// <param name="message"></param>
    /// <param name="subId"></param>
    /// <returns></returns>
    internal static bool Send(long id, IMessage message, long subId)
    {
        RwLock.EnterReadLock();
        try
        {
            if (!Services.TryGetValue(id, out IServiceContext? ctx))
                return false;
            ctx.Receive(new Msg(0, message, subId));
            return true;
        }
        finally
        {
            RwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// 进行Req消息调用
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="subId"></param>
    /// <returns></returns>
    internal static ValueTask<IResponse> Call(long id, IRequest request, long subId = 0)
    {
        RwLock.EnterReadLock();
        try
        {
            if (!Services.TryGetValue(id, out IServiceContext? ctx))
            {
                return ValueTask.FromException<IResponse>(new InvalidServiceIdException(id));
            }

            int rpcId = request.RpcId = Interlocked.Increment(ref _rpcIdGen);
            var tcs = ResponseTcs.Create();
            if (!Callbacks.TryAdd(request.RpcId, tcs))
            {
                return ValueTask.FromException<IResponse>(new DuplicateRpcIdException(rpcId));
            }

            ctx.Receive(new Msg(0, request, subId));
            return tcs.Task;
        }
        finally
        {
            RwLock.ExitReadLock();
        }
    }

    internal static void Reply(IResponse resp)
    {
        if (!Callbacks.TryRemove(resp.RpcId, out ResponseTcs? tcs))
            throw new Exception($"回应的RpcId不存在: {resp.RpcId} - {resp}");
        tcs.SetResult(resp);

        #region 不用检测，因为Stop时也要能收到响应消息

        // 不在同个上下文中，才检测需要检查一下fiber是否存在; 在同个上下文中，也要投递一下，不进行递归的调用
        // if (tcs.SynchronizationContext != SynchronizationContext.Current && tcs.SynchronizationContext is ServiceContext ctx)
        // {
        //     RwLock.EnterReadLock();
        //     try
        //     {
        //         if (!Services.ContainsKey(ctx.Id))
        //             return;
        //     }
        //     finally
        //     {
        //         RwLock.ExitReadLock();
        //     }
        // }

        #endregion
    }

    internal static void Reply(int rpcId, Exception ex)
    {
        if (!Callbacks.TryRemove(rpcId, out ResponseTcs? tcs))
            throw new Exception($"回应的RpcId不存在: {rpcId} - {ex}");
        tcs.SetException(ex);
    }

    #endregion

    public static ServiceId GetId(this IService self)
    {
        SynchronizationContext? ctx = SynchronizationContext.Current;
        if (ctx is not ServiceContext serviceContext)
            throw new InvalidOperationException("只能在Service上下文中使用.");
        return serviceContext.Id;
    }
}