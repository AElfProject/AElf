using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.PoA.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.PoA;

[DependsOn(
    typeof(RxNetSchedulerAElfModule),
    typeof(CoreConsensusAElfModule)
)]
public class PoAAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IConsensusExtraDataExtractor, PoAExtraDataExtractor>();
        context.Services.AddSingleton<IBroadcastPrivilegedPubkeyListProvider, PoABroadcastPrivilegedPubkeyListProvider>();
    }
}