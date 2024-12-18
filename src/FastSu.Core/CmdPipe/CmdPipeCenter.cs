using System.Reflection;

namespace FastSu.Core;

public sealed class CmdPipeCenter : Singleton<CmdPipeCenter>, IAssemblyPostProcess
{
    private Dictionary<long, ICmdPipe>? _pipes;
    private Dictionary<long, ICmdPipe>? _tmp;

    private CmdPipeCenter()
    {
    }

    public void Begin()
    {
        _tmp = new Dictionary<long, ICmdPipe>();
    }

    private static long CombineId(int id, ushort pipeId)
    {
        return id << 16 | pipeId;
    }

    public void Process(Type type, bool isHotfix)
    {
        if (type.IsAbstract || type.IsInterface)
            return;

        PipeAttribute? attr = type.GetCustomAttribute<PipeAttribute>();
        if (attr is null) return;

        object? obj = Activator.CreateInstance(type);
        if (obj is not ICmdPipe cmdPipe)
        {
            SLog.Warn($"CmdPipe没有继承ACmdPipe: {type.FullName}");
            return;
        }

        // 拼接id
        long id = CombineId(cmdPipe.Id, attr.Id);
        if (!_tmp!.TryAdd(id, cmdPipe))
        {
            SLog.Warn($"CmdPipe重复: typeId={cmdPipe.Id} pipeId={attr.Id} {type.FullName}");
        }
        else
        {
            SLog.Debug($"注册CmdPipe: typeId={cmdPipe.Id} pipeId={attr.Id} {type.FullName}");
        }
    }

    public void End()
    {
        Interlocked.Exchange(ref _pipes, _tmp);
        _tmp = null;
    }

    internal ICmdPipe? GetPipe(int typeId, ushort pipeId)
    {
        if (_pipes is null)
            return null;
        long id = CombineId(typeId, pipeId);
        _pipes.TryGetValue(id, out ICmdPipe? pipe);
        return pipe;
    }
}