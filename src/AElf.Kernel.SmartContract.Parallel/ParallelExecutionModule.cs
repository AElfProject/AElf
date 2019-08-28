using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Parallel
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ParallelExecutionModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}