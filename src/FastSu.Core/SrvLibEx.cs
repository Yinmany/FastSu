namespace FastSu;

public static class SrvLibEx
{
    /// <summary>
    /// 添加SrvLib核心后处理
    ///     1. CmdPipeCenter
    ///     2. ServiceEventCenter
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static AssemblyPartManager AddCoreProcess(this AssemblyPartManager self)
    {
        return self
            .AddPostProcess(CmdPipeCenter.Ins)
            .AddPostProcess(ServiceEventCenter.Ins)
            .AddPostProcess(MessageHandlers.Ins);
    }
}