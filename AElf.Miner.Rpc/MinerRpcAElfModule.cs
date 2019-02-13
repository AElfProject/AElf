using AElf.Configuration;
using AElf.Miner.Rpc.Server;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Miner.Rpc
{
    [DependsOn(typeof(KernelAElfModule))]
    public class MinerRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<SideChainBlockInfoRpcServer>();
            services.AddSingleton<ParentChainBlockInfoRpcServer>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId
                .ConvertBase58ToChainId();
            context.ServiceProvider.GetService<SideChainBlockInfoRpcServer>().Init(chainId);
            context.ServiceProvider.GetService<ParentChainBlockInfoRpcServer>().Init(chainId);
        }
    }
}