using FastSu.Core;

namespace HelloServer;

public class EchoService : AService<EchoService>
{
    public int Count { get; set; }
}