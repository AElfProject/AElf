using System.Linq;
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
            var configuration = services.GetConfiguration();
            var crossChainConfiguration =
                configuration.GetChildren().FirstOrDefault(child => child.Key.Equals("CrossChain"));
            var grpcCrossChainConfiguration =
                crossChainConfiguration?.GetChildren().FirstOrDefault(child => child.Key.Equals("Grpc"));
            if (grpcCrossChainConfiguration == null)
                return;
            Configure<GrpcCrossChainConfigOption>(grpcCrossChainConfiguration);
            services.AddSingleton<IGrpcClientPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<IGrpcServePlugin, GrpcCrossChainServerNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcGrpcCrossChainServer>();
            services.AddTransient<ICrossChainCommunicationController, GrpcCommunicationController>();
        }
    }
}