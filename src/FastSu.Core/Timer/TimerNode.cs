namespace FastSu;



internal class TimerNode : ITimerNode
{
    private TimerCallback? _callback;

    public readonly uint Interval;

    public ulong Expires; // 最终超时时间
    private Action<TimerNode>? _disposableAction;

    public TimerNode? Prev, Next;
    public TimerLinkedList? List;

    public int Type { get; }
    public object? State { get; }

    public TimerNode(ulong expires, object? state, Action<TimerNode> disposableAction, int type, uint interval, TimerCallback callback)
    {
        this.Expires = expires;
        this.State = state;
        this._disposableAction = disposableAction;
        this.Type = type;
        this.Interval = interval;
        this._callback = callback;
    }

    public void Invoke() => _callback?.Invoke(this);

    /// <summary>
    /// 会在其它线程进行操作
    /// </summary>
    public void Dispose()
    {
        // 先把回调设置为null，保证调用释放后，就不会执行回调了(释放并不是马上执行的)
        if (Interlocked.Exchange(ref _callback, null) == null)
            return;

        var tmp = Interlocked.Exchange(ref _disposableAction, null);
        tmp?.Invoke(this);
    }
}