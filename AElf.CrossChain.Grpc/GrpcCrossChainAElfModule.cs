using AElf.CrossChain.Grpc.Server;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CrossChainAElfModule))]
    public class GrpcCrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<CrossChainGrpcServer>();
            var configuration = context.Services.GetConfiguration();
            Configure<CrossChainGrpcConfigOption>(configuration.GetSection("CrossChainGrpc"));
        }
    }
}