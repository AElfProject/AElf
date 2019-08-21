using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    public class ConsensusAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
            context.Services.AddTransient<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddTransient<IBlockValidationProvider, ConsensusValidationProvider>();
            context.Services.AddSingleton<BestChainFoundEventHandler>();
            context.Services.AddSingleton<IIsPackageNormalTransactionProvider, IsPackageNormalTransactionProvider>();
        }
    }
}