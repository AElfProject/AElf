using System.Linq;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
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
            var configuration = services.GetConfiguration();
            var crossChainConfiguration = configuration.GetSection("CrossChain");
            var grpcCrossChainConfiguration = crossChainConfiguration.GetSection("Grpc");
//            if (!grpcCrossChainConfiguration.Exists())
//                return;
            Configure<GrpcCrossChainConfigOption>(grpcCrossChainConfiguration);
            services.AddSingleton<IGrpcClientPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<IGrpcServePlugin, GrpcCrossChainServerNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();
            services.AddTransient<ICrossChainCommunicationController, GrpcCommunicationController>();
        }
    }
}