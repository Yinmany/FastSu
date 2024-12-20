using Cysharp.Threading.Tasks;

namespace FastSu;

/// <summary>
/// 全局管道
/// </summary>
public static class GlobalCmdPipe
{
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static UniTask Execute(in Cmd cmd, object? ctx = default)
    {
        ICmdPipe? pipe = CmdPipeCenter.Ins.GetPipe(0, cmd.PipeId);
        if (pipe is AGlobalCmdPipe cmdPipe)
            return cmdPipe.Execute(ctx, cmd);
        return UniTask.CompletedTask;
    }
}

public readonly struct CmdPipe<T>
{
    public readonly int Id = TypeId.Get(typeof(T));
    private readonly T? _ctx;

    public CmdPipe(T? ctx = default)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// 使用CmdPipe派发命令
    /// </summary>
    /// <returns></returns>
    public UniTask Execute(in Cmd cmd, T? ctx = default)
    {
        ICmdPipe? pipe = CmdPipeCenter.Ins.GetPipe(Id, cmd.PipeId);
        if (pipe is ACmdPipe<T> cmdPipe)
            return cmdPipe.Execute(ctx ?? _ctx, cmd);
        return UniTask.CompletedTask;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PipeAttribute(ushort id) : Attribute
{
    public readonly ushort Id = id;
}

/// <summary>
/// 一个命令管道处理器
/// </summary>
internal interface ICmdPipe
{
    int Id { get; }
}

/// <summary>
/// 命令管道处理器
/// </summary>
public abstract class ACmdPipe<T> : ICmdPipe
{
    public int Id => TypeId.Get(typeof(T));

    protected ACmdPipe()
    {
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cmd">命令</param>
    /// <returns></returns>
    public abstract UniTask Execute(T? ctx, Cmd cmd);
}

/// <summary>
/// 全局命令管道处理器
/// </summary>
public abstract class AGlobalCmdPipe : ICmdPipe
{
    public int Id => 0;

    protected AGlobalCmdPipe()
    {
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cmd">命令</param>
    /// <returns></returns>
    public abstract UniTask Execute(object? ctx, Cmd cmd);
}