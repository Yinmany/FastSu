using System.Net;
using FastSu.Server.Network;
using FastSu.Server.Rpc;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;

namespace FastSu.Server;

public static class FastSuHost
{
    /// <summary>
    /// 创建一个Empty HostBuilder;
    ///     1. 添加NLog
    ///     2. 使用DOTNET_ENVIRONMENT作为环境变量
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static HostApplicationBuilder Create(string[]? args = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            Args = args,
            EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            ApplicationName = $"FastSu Server",
        });

        builder.Logging.AddNLog();
        return builder;
    }

    public static IServiceCollection AddFastSu(this IServiceCollection services, ushort pid, IPEndPoint? bindIp = null)
    {
        services.Configure<ServerOptions>(f =>
        {
            f.Pid = pid;
            f.BindEndPoint = bindIp;
        });

        // .Configure<SocketTransportOptions>(o => { o.IOQueueCount = 1; })

        // rpc
        services.AddSocketConnectionFactory();
        services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();

        services.AddSingleton<InternalNetwork>();
        services.AddSingleton<IRpc>(f => f.GetRequiredService<InternalNetwork>());
        services.TryAddSingleton<IRpcSerializer, ProtobufRpcSerializer>();

        services.AddHostedService<FastSuHostedService>();
        return services;
    }
}