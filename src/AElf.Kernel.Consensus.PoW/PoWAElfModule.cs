using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.PoW.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.PoW
{
    [DependsOn(
        typeof(RxNetSchedulerAElfModule),
        typeof(ConsensusAElfModule)
    )]
    public class PoWAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITriggerInformationProvider, PoWTriggerInformationProvider>();

        }
    }
}