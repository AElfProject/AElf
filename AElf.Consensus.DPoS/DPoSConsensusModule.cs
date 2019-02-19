using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusInformationGenerationService, DPoSInformationGenerationService>();
            context.Services.AddSingleton<IConsensusObserver, DPoSObserver>();
            
            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<IConsensusCommand, ConsensusCommand>();
        }
    }
}