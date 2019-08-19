using AElf.Kernel.Consensus.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class RxNetSchedulerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusScheduler, RxNetScheduler>();
        }
    }
}