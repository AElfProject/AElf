using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus;

[DependsOn(typeof(ConfigurationAElfModule))]
public class CoreConsensusAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
        context.Services.AddTransient<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
        context.Services.AddSingleton<IConsensusExtraDataProvider, ConsensusExtraDataProvider>();
        context.Services.AddTransient<IBlockValidationProvider, ConsensusValidationProvider>();
        context.Services.AddTransient(typeof(IConfigurationProcessor),
            typeof(MiningTimeConfigurationProcessor));
    }
}