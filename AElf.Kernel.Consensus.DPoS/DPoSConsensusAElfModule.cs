using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS
{
    [DependsOn(
        typeof(RxNetSchedulerAElfModule),
        typeof(ConsensusAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSConsensusAElfModule>();

            context.Services.AddScoped<ISmartContractAddressNameProvider, ConsensusSmartContractAddressNameProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();

            context.Services.AddTransient<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddTransient<IBlockValidationProvider, ConsensusValidationProvider>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, DPoSInformationGenerationService>();

            context.Services.AddSingleton<BestChainFoundEventHandler>();
        }
    }
}