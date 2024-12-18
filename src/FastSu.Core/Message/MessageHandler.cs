using Cysharp.Threading.Tasks;

namespace FastSu.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MessageHandlerAttribute : Attribute
{
}

internal interface IMsgHandlerBase
{
    int Id { get; }

    int MsgId { get; }
}

internal interface IMsgHandler : IMsgHandlerBase
{
    UniTask Invoke(object self, IMessage msg);
}

public delegate void RpcReplyAction(IResponse response);

internal interface IReqHandler : IMsgHandlerBase
{
    UniTask Invoke(object self, IRequest req, RpcReplyAction reply);
}

public abstract class MsgHandler<T, TMsg> : IMsgHandler where TMsg : IMessage
{
    public int Id => TypeId.Get(typeof(T));
    public int MsgId => MessageTypes.ReflectionGetMsgId(typeof(TMsg));

    public UniTask Invoke(object self, IMessage msg) => On((T)self, (TMsg)msg);

    protected abstract UniTask On(T self, TMsg msg);
}

public abstract class ReqHandler<T, TReq, TResult> : IReqHandler where TReq : IRequest where TResult : IResponse
{
    public int Id => TypeId.Get(typeof(T));
    public int MsgId => MessageTypes.ReflectionGetMsgId(typeof(TReq));

    protected readonly struct Reply(RpcReplyAction callback, int rpcId)
    {
        public void Send(TResult res)
        {
            res.RpcId = rpcId;
            callback(res);
        }
    }

    public UniTask Invoke(object self, IRequest req, RpcReplyAction reply) => On((T)self, (TReq)req, new Reply(reply, req.RpcId));

    protected abstract UniTask On(T self, TReq req, Reply reply);
}