namespace FastSu.Core;

public abstract class Singleton<T> where T : Singleton<T>
{
    public static readonly T Ins = (T)Activator.CreateInstance(typeof(T), true)!;
}