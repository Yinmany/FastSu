using System.Net;
using FastSu;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FastSu.Server;

public class ServerOptions
{
    public ushort Pid { get; internal set; }
    public IPEndPoint? BindEndPoint { get; internal set; }
}