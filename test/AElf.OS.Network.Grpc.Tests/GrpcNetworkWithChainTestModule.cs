using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc;

[DependsOn(
    typeof(OSCoreWithChainTestAElfModule),
    typeof(GrpcNetworkBaseTestModule))]
public class GrpcNetworkWithChainTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var syncStateProvider = context.ServiceProvider.GetRequiredService<INodeSyncStateProvider>();
        syncStateProvider.SetSyncTarget(-1);
    }
}