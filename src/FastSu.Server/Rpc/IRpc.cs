using System.Net;
using FastSu;

namespace FastSu.Server.Rpc;

/// <summary>
/// 用于内部服务通信
/// </summary>
public interface IRpc
{
    void Register(ushort pid, IPEndPoint ipEndPoint);

    void Unregister(ushort pid);

    void Send(long serviceId, IMessage msg, long subId = 0);

    ValueTask<IResponse> Call(long serviceId, IRequest request, long subId = 0);
}