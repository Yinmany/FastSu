using FastSu;

namespace HelloServer;

public class StartupService : AService<StartupService>
{
    public ITimerNode PingTimer { get; set; }
    public ServiceId EchoId { get; set; }
}