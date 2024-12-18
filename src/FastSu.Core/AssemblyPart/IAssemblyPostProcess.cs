namespace FastSu.Core;

public interface IAssemblyPostProcess
{
    void Begin();

    void Process(Type type, bool isHotfix);

    void End();
}