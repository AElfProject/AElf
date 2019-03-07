using AElf.Kernel.Tests;
using AElf.Modularity;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(AbpEventBusModule),
        typeof(CrossChainAElfModule),
        typeof(KernelTestAElfModule))]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO: please mock data here, do not directly new object, if you have multiple dependency, you should have 
            //different modules, like  AElfIntegratedTest<AAACrossChainTestModule>,  AElfIntegratedTest<BBBCrossChainTestModule>
        }
    }
}