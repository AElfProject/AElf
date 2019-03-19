using AElf.Modularity;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelCoreCreateChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}