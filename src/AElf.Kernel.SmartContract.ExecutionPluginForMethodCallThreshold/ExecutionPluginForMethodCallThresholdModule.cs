using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodCallThreshold
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForMethodCallThresholdModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}