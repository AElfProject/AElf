using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc;

public class GrpcNetworkModule : AElfModule
{
    /// <summary>
    ///     Registers the components implemented by the gRPC library.
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();
        context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();

        // Internal dependencies
        context.Services.AddTransient<IPeerDialer, PeerDialer>();
        context.Services.AddSingleton<GrpcServerService>();

        context.Services.AddSingleton<AuthInterceptor>();
        context.Services.AddSingleton<RetryInterceptor>();

        ConfigureStreamMethods(context);
    }

    private void ConfigureStreamMethods(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IStreamMethod, GetNodesMethod>();
        context.Services.AddSingleton<IStreamMethod, HealthCheckMethod>();
        context.Services.AddSingleton<IStreamMethod, PingMethod>();
        context.Services.AddSingleton<IStreamMethod, DisconnectMethod>();
        context.Services.AddSingleton<IStreamMethod, ConfirmHandShakeMethod>();
        context.Services.AddSingleton<IStreamMethod, RequestBlockMethod>();
        context.Services.AddSingleton<IStreamMethod, RequestBlocksMethod>();
        context.Services.AddSingleton<IStreamMethod, BlockBroadcastMethod>();
        context.Services.AddSingleton<IStreamMethod, AnnouncementBroadcastMethod>();
        context.Services.AddSingleton<IStreamMethod, TransactionBroadcastMethod>();
        context.Services.AddSingleton<IStreamMethod, LibAnnouncementBroadcastMethod>();
    }
}