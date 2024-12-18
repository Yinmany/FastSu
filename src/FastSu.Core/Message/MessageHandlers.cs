using System.Collections.Immutable;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace FastSu.Core;

internal sealed class MessageHandlers : Singleton<MessageHandlers>, IAssemblyPostProcess
{
    private ImmutableDictionary<long, IMsgHandlerBase>? _handler;
    private Dictionary<long, IMsgHandlerBase>? _tmp;

    private bool _isLoadedMsgType = false;

    private MessageHandlers()
    {
    }

    void IAssemblyPostProcess.Begin()
    {
        _tmp = new Dictionary<long, IMsgHandlerBase>();
    }

    void IAssemblyPostProcess.Process(Type type, bool isHotfix)
    {
        if (type.IsAbstract || type.IsInterface)
            return;

        if (!_isLoadedMsgType)
        {
            if (MessageTypes.Ins.Add(type))
                return;
        }

        var attr = type.GetCustomAttribute<MessageHandlerAttribute>();
        if (attr is null)
            return;

        IMsgHandlerBase? handler = Activator.CreateInstance(type) as IMsgHandlerBase;
        if (handler is null)
        {
            SLog.Error($"消息处理器创建失败: {type.FullName} 必须实现 {nameof(IMsgHandlerBase)} 接口");
            return;
        }

        _tmp!.Add(GetHandlerId(handler.Id, handler.MsgId), handler);
    }

    static long GetHandlerId(int typeId, int msgId)
    {
        return (long)((ulong)typeId << 32 | (uint)msgId);
    }

    void IAssemblyPostProcess.End()
    {
        if (!_isLoadedMsgType)
        {
            _isLoadedMsgType = true;
            MessageTypes.Ins.CheckThrowException();
        }

        _handler = _tmp!.ToImmutableDictionary();
        _tmp = null;
    }

    /// <summary>
    /// 分发一个消息
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ctx"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public UniTask Dispatch(int id, object ctx, IMessage msg)
    {
        long handlerId = GetHandlerId(id, msg.MsgId);
        if (!_handler!.TryGetValue(handlerId, out IMsgHandlerBase? handler))
        {
            SLog.Error($"消息处理器不存在: {id}/{msg.MsgId} {msg.GetType().FullName}");
            return UniTask.CompletedTask;
        }

        return ((IMsgHandler)handler).Invoke(ctx, msg);
    }

    /// <summary>
    /// 分发一个请求
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ctx"></param>
    /// <param name="req"></param>
    /// <param name="reply"></param>
    /// <returns></returns>
    public UniTask Dispatch(int id, object ctx, IRequest req, RpcReplyAction reply)
    {
        long handlerId = GetHandlerId(id, req.MsgId);
        if (!_handler!.TryGetValue(handlerId, out IMsgHandlerBase? handler))
        {
            SLog.Error($"消息处理器不存在: {id}/{req.MsgId} {req.GetType().FullName}");
            return UniTask.CompletedTask;
        }

        return ((IReqHandler)handler).Invoke(ctx, req, reply);
    }
}