namespace FastSu;

/// <summary>
/// 全局的定时器
/// </summary>
public static class STimer
{
    private static readonly TimerWheel Tw = new TimerWheel();
    private static readonly TimerCallback Callback = ServiceTimerCallback;

    public static ITimerNode AddTimeout(TimeSpan dueTime, TimerCallback action, int type = 0, object? state = null)
    {
        return Tw.AddTimeout((uint)dueTime.TotalMilliseconds, action, type, state);
    }

    public static ITimerNode AddInterval(TimeSpan dueTime, TimeSpan period, TimerCallback action, int type = 0, object? state = null)
    {
        return Tw.AddInterval((uint)dueTime.TotalMilliseconds, (uint)period.TotalMilliseconds, action, type, state);
    }

    public static ITimerNode AddTimeout(this IService self, TimeSpan dueTime, int type = 0)
    {
        ServiceContext? ctx = SynchronizationContext.Current as ServiceContext;
        if (ctx == null || ctx.ServiceInstance != self)
            throw new Exception("只能在同个Service上下文中使用.");

        return AddTimeout(dueTime, Callback, type, ctx);
    }

    public static ITimerNode AddInterval(this IService self, TimeSpan dueTime, TimeSpan period, int type = 0)
    {
        ServiceContext? ctx = SynchronizationContext.Current as ServiceContext;
        if (ctx == null || ctx.ServiceInstance != self)
            throw new Exception("只能在同个Service上下文中使用.");
        return AddInterval(dueTime, period, Callback, type, ctx);
    }

    private static void ServiceTimerCallback(ITimerNode timer)
    {
        IServiceContext iServiceCtx = (IServiceContext)timer.State!;
        iServiceCtx.Post(SendOrPostCallback, timer); // 转投到上下文中
    }

    static void SendOrPostCallback(object? o)
    {
        ITimerNode timer = (ITimerNode)o!;
        ((IServiceContext)timer.State!).ServiceInstance.OnTimer(timer);
    }
}