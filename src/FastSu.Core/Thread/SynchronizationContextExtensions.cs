using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastSu;

public static class SynchronizationContextExtensions
{
    /// <summary>
    /// 切到当前同步上线文中执行
    /// </summary>
    /// <returns></returns>
    public static Awaiter Yield(this SynchronizationContext self) => new Awaiter(self);

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public readonly struct Awaiter(SynchronizationContext synchronizationContext) : INotifyCompletion
    {
        private static readonly SendOrPostCallback SendOrPostCallbackDelegate = new(Continuation);

        public bool IsCompleted => false;

        public void GetResult()
        {
        }

        public Awaiter GetAwaiter() => this;

        public void OnCompleted(Action continuation)
        {
            // 同个上线文，直接执行；
            if (SynchronizationContext.Current == synchronizationContext)
            {
                continuation();
            }
            else
            {
                synchronizationContext.Post(SendOrPostCallbackDelegate, continuation);
            }
        }

        private static void Continuation(object? state) => ((Action)state!)();
    }
}