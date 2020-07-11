using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.AEPoW
{
    [DependsOn(
        typeof(RxNetSchedulerAElfModule),
        typeof(CoreConsensusAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class AEPoWAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services
                .AddSingleton<IContractInitializationProvider, DefaultConsensusContractInitializationProvider>();
        }
    }
}