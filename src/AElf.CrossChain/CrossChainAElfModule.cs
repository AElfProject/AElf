using AElf.CrossChain.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Infrastructure;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CrossChainCoreModule))]
    [DependsOn(typeof(NodeAElfModule))]
    public class CrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, CrossChainTransactionGenerator>();
            context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddSingleton<IChainInitializationDataPlugin, CrossChainPlugin>();
            context.Services.AddSingleton<INodePlugin, CrossChainPlugin>();
        }
    }
}