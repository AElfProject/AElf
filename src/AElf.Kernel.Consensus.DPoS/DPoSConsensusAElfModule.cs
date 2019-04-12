using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.Consensus.Scheduler.FluentScheduler;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using BestChainFoundEventHandler = AElf.Kernel.Consensus.Application.BestChainFoundEventHandler;

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
            context.Services.AddSingleton<IIrreversibleBlockDiscoveryService, IrreversibleBlockDiscoveryService>();
            context.Services.AddSingleton<IDPoSInformationProvider, DPoSInformationProvider>();
            context.Services.AddSingleton<BestChainFoundEventHandler>();

            var configuration = context.Services.GetConfiguration();

            Configure<DPoSOptions>(configuration.GetSection("Consensus"));

        }
    }
}