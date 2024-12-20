using System.Runtime.CompilerServices;

namespace FastSu;

/// <summary>
/// 服务句柄(用于外部执行对服务的操作)
/// </summary>
/// <param name="id"></param>
public readonly struct ServiceId(long id)
{
    public readonly long Id = id;

    public override string ToString()
    {
        // 转为可读的id
        Did id = new Did(Id);
        return $"{id.Pid}/{id.Time}-{id.Seq}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Send(in Msg msg) => ServiceMgr.Post(Id, msg);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="subId"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Send(IMessage message, long subId = 0) => ServiceMgr.Send(Id, message, subId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="subId"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IResponse> Call(IRequest request, long subId = 0) => ServiceMgr.Call(Id, request, subId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reply(IResponse resp) => ServiceMgr.Reply(resp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reply(int rpcId, Exception ex) => ServiceMgr.Reply(rpcId, ex);

    /// <summary>
    /// 隐式转换
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator ServiceId(long id) => new(id);

    /// <summary>
    /// 本地id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceId LocalId(ushort id) => Did.Make(id);
}