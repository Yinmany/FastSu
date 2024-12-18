using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;

namespace FastSu.Core;

/// <summary>
/// 总共16个字节
/// </summary>
[StructLayout(LayoutKind.Auto, Pack = 4)]
public readonly record struct Cmd
{
    public readonly ushort PipeId;
    public readonly ushort CmdId;
    public readonly uint SrcId;
    public readonly object? Arg1;
    public readonly object? Arg2;

    /// <summary>
    /// Fiber专属
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    internal Cmd(object arg1, object arg2)
    {
        this.Arg1 = arg1;
        this.Arg2 = arg2;
    }

    public Cmd(ushort pipeId, ushort cmdId, uint srcId = 0, object? arg1 = null, object? arg2 = null)
    {
        this.PipeId = pipeId;
        if (pipeId == 0) throw new InvalidOperationException($"pipeId不能为0");

        this.CmdId = cmdId;
        this.SrcId = srcId;
        this.Arg1 = arg1;
        this.Arg2 = arg2;
    }
}