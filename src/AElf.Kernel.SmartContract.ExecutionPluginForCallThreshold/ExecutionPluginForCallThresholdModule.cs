using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForCallThresholdModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}