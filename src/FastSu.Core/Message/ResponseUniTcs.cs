using System.Threading.Tasks.Sources;
using FastSu.Utils;

namespace FastSu.Core;

/// <summary>
/// 使用 ValueTaskSource 的实现(因为需要捕获同步上下文)
/// </summary>
public class ResponseTcs : IValueTaskSource<IResponse>, ISObjectPoolNode<ResponseTcs>
{
    private static SObjectPool<ResponseTcs> pool;
    private ResponseTcs? _nextNode;

    ref ResponseTcs? ISObjectPoolNode<ResponseTcs>.NextNode => ref _nextNode;

    private ManualResetValueTaskSourceCore<IResponse> _core;

    public ValueTask<IResponse> Task => new(this, _core.Version);

    public IResponse GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            _core.Reset();
            pool.TryPush(this);
        }
    }

    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }

    public void SetResult(IResponse response) => _core.SetResult(response);
    public void SetException(Exception exception) => _core.SetException(exception);

    public static ResponseTcs Create()
    {
        if (!pool.TryPop(out var result)) // pool中没有，就new一个
            result = new ResponseTcs();
        return result!;
    }
}