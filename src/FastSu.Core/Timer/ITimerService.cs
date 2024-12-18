namespace FastSu.Core;

public interface ITimerNode : IDisposable
{
    int Type { get; }

    object? State { get; }
}

/// <summary>
/// 定时器回调
/// </summary>
public delegate void TimerCallback(ITimerNode timerNode);

/// <summary>
/// 定时器服务
/// </summary>
public interface ITimerService
{
    /// <summary>
    /// 执行一次的定时器
    /// </summary>
    /// <param name="dueTime">到期时间;调用之前要延迟的时间量（以毫秒为单位）。指定零 （0） 以立即启动计时器</param>
    /// <param name="action"></param>
    /// <param name="type"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    ITimerNode AddTimeout(uint dueTime, TimerCallback action, int type = 0, object? state = null);

    /// <summary>
    /// 周期执行的定时器
    /// </summary>
    /// <param name="dueTime">到期时间;调用之前要延迟的时间量（以毫秒为单位）。指定零 （0） 以立即启动计时器</param>
    /// <param name="period">调用 之间的时间间隔 ，以毫秒为单位。指定 Infinite 以禁用定期信号</param>
    /// <param name="action"></param>
    /// <param name="type"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    ITimerNode AddInterval(uint dueTime, uint period, TimerCallback action, int type = 0, object? state = null);
}