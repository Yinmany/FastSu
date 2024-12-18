namespace FastSu.Core;

/// <summary>
/// Rpc 错误码(1-100)
/// </summary>
public static class RpcErrorCode
{
    public const int Success = 0;

    /// <summary>
    /// 异常
    /// </summary>
    public const int Exception = 1;

    /// <summary>
    /// 找不到指定服务
    /// </summary>
    public const int ServiceNotFound = 2;

    /// <summary>
    /// 判断是否需要抛出异常
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static bool IsThrow(int code) => code is > 1 and <= 100;
}