namespace FastSu.Core;

public sealed class WorkThread
{
    private readonly Thread _thread;

    public event Action? OnTick;

    public WorkThread()
    {
        _thread = new Thread(Tick)
        {
            IsBackground = true
        };
        _thread.Start();
    }

    private void Tick()
    {
        while (true)
        {
            Thread.Sleep(1);
            OnTick?.Invoke();
        }
    }
}