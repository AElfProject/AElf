using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    [DependsOn(typeof(CrossChainCommunicationModule))]
    public class GrpcCrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IGrpcClientPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<IGrpcServePlugin, GrpcCrossChainServerNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();
            services.AddTransient<ICrossChainCommunicationController, GrpcCommunicationController>();
            
            var grpcCrossChainConfiguration = services.GetConfiguration().GetSection("CrossChain");
            Configure<GrpcCrossChainConfigOption>(grpcCrossChainConfiguration.GetSection("Grpc"));
        }
    }
}