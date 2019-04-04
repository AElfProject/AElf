using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    [DependsOn(typeof(DPoSConsensusAElfModule)), DependsOn(typeof(CoreOSAElfModule))]
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusOSAElfModule : AElfModule<DPoSConsensusOSAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<DPoSAnnouncementReceivedEventDataHandler>();
            context.Services
                .AddSingleton<IDPoSLastLastIrreversibleBlockDiscoveryService,
                    DPoSLastLastIrreversibleBlockDiscoveryService>();
        }
    }
}