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
                option.LocalServerPort = 5001;
                option.LocalServerHost = "127.0.0.1";
                option.RemoteParentChainServerHost = "127.0.0.1";
                option.RemoteParentChainServerPort = 5000;
            });
            
            Configure<ChainOptions>(option =>
            {
                option.ChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            });
            
            services.AddTransient<IGrpcClientPlugin, GrpcCrossChainClientNodePlugin>();
            services.AddTransient<IGrpcServePlugin, GrpcCrossChainServerNodePlugin>();
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();
            services.AddSingleton<GrpcCrossChainClientNodePlugin>();
        }
    }
}