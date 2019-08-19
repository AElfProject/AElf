using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    [DependsOn(typeof(AEDPoSAElfModule)), DependsOn(typeof(CoreOSAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    // ReSharper disable once InconsistentNaming
    public class AElfConsensusOSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<DPoSAnnouncementReceivedEventDataHandler>();
        }
    }
}