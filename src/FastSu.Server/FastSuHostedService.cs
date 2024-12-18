using System.Net;
using FastSu.Core;
using FastSu.Server.Rpc;
using FastSu.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FastSu.Server;

internal class FastSuHostedService : IHostedService
{
    private readonly SLogger _logger = new("FastSu");
    private readonly IOptions<ServerOptions> _serverOptions;
    private readonly InternalNetwork _internalNetwork;

    public FastSuHostedService(IOptions<ServerOptions> serverOptions, InternalNetwork internalNetwork)
    {
        _serverOptions = serverOptions;
        _internalNetwork = internalNetwork;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AssemblyPartManager.Ins.AddCoreProcess().Load();
        Did.Init(_serverOptions.Value.Pid);

        IPEndPoint? bindIp = _serverOptions.Value.BindEndPoint;
        if (bindIp != null)
            await _internalNetwork.StartAsync(bindIp, cancellationToken);

        _logger.Info($"Server start: pid={Did.CurPid} time={STime.TsSeconds}");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ServiceMgr.ShutdownAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "ServiceMgr停止异常:");
        }

        await _internalNetwork.StopAsync();
        _logger.Info($"Server stop.");
    }
}