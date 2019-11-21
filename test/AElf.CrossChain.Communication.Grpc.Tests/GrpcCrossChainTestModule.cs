using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    [DependsOn(
        typeof(GrpcCrossChainAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractAElfModule)
    )]
    public class GrpcCrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            Configure<GrpcCrossChainConfigOption>(option =>
            {
                option.ListeningPort = 5001;
                option.ParentChainServerIp = "127.0.0.1";
                option.ParentChainServerPort = 5000;
            });

            Configure<CrossChainConfigOptions>(option => { option.ParentChainId = "AELF"; });
            services.AddSingleton<GrpcCrossChainClientNodePlugin>();
        }
    }
}