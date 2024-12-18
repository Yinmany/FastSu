using Cysharp.Threading.Tasks;

namespace FastSu.Core;

public interface IService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken">启动尚未完成时，调用了停止将会马上触发取消token</param>
    /// <returns></returns>
    UniTask OnStart(CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken">停止耗时达到指定超时时间时，取消token将会触发</param>
    /// <returns></returns>
    UniTask OnStop(CancellationToken cancellationToken);

    /// <summary>
    /// 默认每次调度时触发一次;当使用WorkThread时，每帧都会调用
    /// </summary>
    void OnTick();

    /// <summary>
    /// 定时器
    /// </summary>
    /// <param name="timer"></param>
    void OnTimer(ITimerNode timer);

    /// <summary>
    /// 消息
    /// </summary>
    /// <param name="msg"></param>
    UniTask OnMessage(Msg msg);
}