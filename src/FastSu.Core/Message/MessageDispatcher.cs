using Cysharp.Threading.Tasks;

namespace FastSu;

public readonly struct MessageDispatcher
{
    private readonly int _id;
    private readonly object _obj;

    private MessageDispatcher(object obj, int id)
    {
        _obj = obj;
        _id = id;
    }

    public static MessageDispatcher Global(object obj) => new(obj, 0);

    public static MessageDispatcher Create(object obj) => new(obj, TypeId.Get(obj.GetType()));

    public UniTask Run(IMessage message)
    {
        return MessageHandlers.Ins.Dispatch(_id, _obj, message);
    }

    public UniTask Run(IRequest request, RpcReplyAction reply)
    {
        return MessageHandlers.Ins.Dispatch(_id, _obj, request, reply);
    }
}