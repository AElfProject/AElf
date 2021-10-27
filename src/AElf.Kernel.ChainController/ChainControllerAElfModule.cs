using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(typeof(SmartContractExecutionAElfModule))]
    public class ChainControllerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}