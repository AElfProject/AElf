using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS
{
    [DependsOn(typeof(TypesAElfModule), typeof(DatabaseAElfModule), typeof(CoreAElfModule))]
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSConsensusModule>();

            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<IConsensusScheduler, RxNetScheduler>();

            context.Services.AddScoped<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
            context.Services.AddScoped<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, DPoSInformationGenerationService>();
            
            context.Services.AddSingleton<ConsensusControlInformation>();
        }
    }
}