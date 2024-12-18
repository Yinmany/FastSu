using System.Runtime.CompilerServices;
using FastSu.Utils;

namespace FastSu.Core;

/// <summary>
/// 协程锁(必须在线程安全环境下使用)
/// </summary>
public class CoroutineLock : IDisposable
{
    private readonly Queue<Action> _queue = new();

    public Awaiter GetAwaiter() => new(this);

    /// <summary>
    /// 最近锁定时间
    /// </summary>
    public long? LastLockTime { get; private set; }

    /// <summary>
    /// 是否已锁定
    /// </summary>
    public bool IsLocked => _queue.Count != 0;

    /// <summary>
    /// 在调用Dispose时有两种情况: 1.没有异步逻辑，排队就不存在。 2.有异步逻辑，dispose就会在同步上下文中进行调用。都不会存在递归调用
    /// </summary>
    public void Dispose() // 释放一次锁
    {
        if (_queue.TryDequeue(out _) && _queue.TryPeek(out Action? next))
        {
            LastLockTime = STime.Timestamp;
            next();
        }
        else
        {
            LastLockTime = null; // 没有锁定了
        }
    }

    // internal void Cancel(Exception? e)
    // {
    //     _queue.Clear();
    // }

    public readonly struct Awaiter(CoroutineLock ctx) : INotifyCompletion
    {
        public bool IsCompleted => false;

        public IDisposable GetResult() => ctx;

        public void OnCompleted(Action continuation)
        {
            var queue = ctx._queue;
            queue.Enqueue(continuation);
            if (queue.Count == 1) // 第一个不用排队，需要马上执行。
            {
                continuation();
                ctx.LastLockTime = STime.Timestamp;
            }
        }
    }
}