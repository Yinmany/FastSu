using FastSu;

namespace HelloServer;

public class EchoService : AService<EchoService>
{
    public int Count { get; set; }
}