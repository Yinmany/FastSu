namespace FastSu.Server.Rpc;

public class RpcException : Exception
{
    public const int ErrorNotFoundPid = 1; // 找不到目标进程
    public const int ErrorTimeout = 2; // 请求超时
    public const int ErrorDisconnect = 3; // 连接已断开
    public const int ErrorDuplicateRpcId = 4; // rpcId重复

    public static readonly RpcException NotFoundPid = new(ErrorNotFoundPid, "找不到目标进程.");

    /// <summary>
    /// 连接已断开
    /// </summary>
    public static readonly RpcException Disconnect = new(ErrorDisconnect, "连接已断开.");

    public static readonly RpcException Timeout = new(ErrorTimeout, "请求超时.");

    // rpcId重复
    public static readonly RpcException DuplicateRpcId = new(ErrorDuplicateRpcId, "rpcId重复.");

    public int ErrorCode { get; private set; }

    public RpcException(int errorCode)
    {
        this.ErrorCode = errorCode;
    }

    public RpcException(int errorCode, string msg) : base(msg)
    {
        this.ErrorCode = errorCode;
    }
}