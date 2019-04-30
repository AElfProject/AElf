using AElf.Kernel.Consensus.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetSchedulerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusScheduler, RxNetScheduler>();
        }
    }
}