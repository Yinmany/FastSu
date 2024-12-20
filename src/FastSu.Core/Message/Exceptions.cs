namespace FastSu;

/// <summary>
/// 无效的服务Id异常
/// </summary>
public class InvalidServiceIdException(ServiceId serviceId) : Exception($"invalid service id {serviceId}")
{
    public readonly ServiceId ServiceId = serviceId;
}

/// <summary>
/// RpcId重复异常
/// </summary>
/// <param name="rpcId"></param>
public class DuplicateRpcIdException(int rpcId) : Exception($"duplicate rpc id {rpcId}");