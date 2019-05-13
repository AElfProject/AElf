using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(AEDPoSAElfModule),
        typeof(KernelTestAElfModule)
    )]
    public class OSTestBaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<OSTestHelper>();
        }
    }
}