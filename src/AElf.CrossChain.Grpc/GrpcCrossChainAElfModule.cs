using System.Linq;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;
using AElf.Kernel.Node.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
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
            if (crossChainConfiguration == null)
                return;
            var grpcCrossChainConfiguration =
                crossChainConfiguration.GetChildren().FirstOrDefault(child => child.Key.Equals("Grpc"));
            if(grpcCrossChainConfiguration == null)
                return;
            Configure<GrpcCrossChainConfigOption>(grpcCrossChainConfiguration);
            services.AddTransient<INodePlugin, GrpcCrossChainServerNodePlugin>();
            services.AddTransient<INodePlugin, GrpcCrossChainClientNodePlugin>();
            services.AddSingleton<ICrossChainServer, CrossChainGrpcServer>();
            services.AddSingleton<GrpcCrossChainClientNodePlugin>();
            var keyStore = new CertificateStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<ICertificateStore>(keyStore);
        }
    }
}