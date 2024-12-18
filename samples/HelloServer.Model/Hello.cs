using FastSu.Server;
using FastSu.Server.Rpc;

namespace HelloServer;

/// <summary>
/// 全局共享模块
/// </summary>
public static class Hello
{
    [FacadeInject] public static IRpc Rpc { get; private set; }
}