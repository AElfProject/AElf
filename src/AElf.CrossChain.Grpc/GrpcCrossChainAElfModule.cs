using AElf.CrossChain.Communication;
using AElf.CrossChain.Grpc.Client;
using AElf.CrossChain.Grpc.Server;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
{
    [DependsOn(typeof(CrossChainCoreModule))]
    public class GrpcCrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IGrpcClientPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<ICrossChainCommunicationPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<ICrossChainCommunicationPlugin, GrpcCrossChainServerNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();
            var grpcCrossChainConfiguration = services.GetConfiguration().GetSection("CrossChain");
            Configure<GrpcCrossChainConfigOption>(grpcCrossChainConfiguration.GetSection("Grpc"));
        }
    }
}