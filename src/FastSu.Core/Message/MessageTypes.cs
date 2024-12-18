using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastSu.Core;

public sealed class MessageTypes : Singleton<MessageTypes>
{
    private readonly Dictionary<int, Type> _msgId2Type = new();

    /// <summary>
    /// 响应创建
    /// </summary>
    private readonly ConcurrentDictionary<int, Func<IResponse>> _factory = new();

    private MessageTypes()
    {
    }

    internal bool Add(Type type)
    {
        if (type.IsAssignableTo(typeof(IMessageBase)))
        {
            int msgId = ReflectionGetMsgId(type);
            _msgId2Type.Add(msgId, type);
            SLog.Debug($"注册消息: {msgId} {type.FullName}");
            return true;
        }

        return false;
    }

    internal void CheckThrowException()
    {
        foreach (var type in _msgId2Type.Values)
        {
            if (type.IsAssignableTo(typeof(IRequest)))
            {
                int ackMsgId = ReflectionGetAckMsgId(type);
                if (!_msgId2Type.ContainsKey(ackMsgId))
                {
                    // 抛出异常
                    throw new Exception($"找不到Req响应类型: {type.FullName} -> {ackMsgId}");
                }
            }
        }
    }

    /// <summary>
    /// 通过请求消息创建对应的响应消息
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public IResponse NewResponse(IRequest request)
    {
        Func<IResponse> newFunc = _factory.GetOrAdd(request.MsgId, static id =>
        {
            Type reqType = Ins.GetById(id)!;
            int ackMsgId = ReflectionGetAckMsgId(reqType);
            Type ackType = Ins.GetById(ackMsgId)!;
            return Expression.Lambda<Func<IResponse>>(Expression.New(ackType)).Compile();
        });

        IResponse response = newFunc();
        response.RpcId = request.RpcId;
        return response;
    }

    /// <summary>
    /// 通过消息id获取Type
    /// </summary>
    /// <param name="msgId"></param>
    /// <returns></returns>
    public Type? GetById(int msgId)
    {
        _msgId2Type.TryGetValue(msgId, out Type? type);
        return type;
    }

    /// <summary>
    /// 反射获取消息Id
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static int ReflectionGetMsgId(Type type)
    {
        int? msgId = ReflectionConstValue(type, "_MsgId_");
        if (msgId is null or 0)
            throw new Exception($"找不到MsgId: {type.FullName}");
        return msgId.Value;
    }

    internal static int ReflectionGetAckMsgId(Type type)
    {
        int? msgId = ReflectionConstValue(type, "_AckMsgId_");
        if (msgId is null or 0)
            throw new Exception($"找不到消息的AckMsgId: {type.FullName}");
        return msgId.Value;
    }

    internal static int? ReflectionConstValue(Type type, string constName)
    {
        FieldInfo? fieldInfo = type.GetField(constName, BindingFlags.Static | BindingFlags.Public);
        if (fieldInfo is null)
            return null;
        return (int)fieldInfo.GetRawConstantValue()!;
    }
}