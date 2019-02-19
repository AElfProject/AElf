using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.DPoS.Infrastructure;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, DPoSInformationGenerationService>();
            context.Services.AddSingleton<IConsensusObserver, DPoSObserver>();
            context.Services.AddSingleton<IConsensusCommand, ConsensusCommand>();
        }
    }
}