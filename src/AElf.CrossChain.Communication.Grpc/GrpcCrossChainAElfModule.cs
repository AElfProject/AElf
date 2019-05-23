using System.Linq;
using AElf.CrossChain.Communication.Grpc;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    [DependsOn(typeof(CrossChainAElfModule))]
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
            services.AddTransient<IGrpcCrossChainPlugin, GrpcCrossChainServerNodePlugin>();
            services.AddTransient<IGrpcCrossChainPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcGrpcCrossChainServer>();
            services.AddSingleton<GrpcCrossChainClientNodePlugin>();
        }
    }
}