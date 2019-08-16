using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForAcs5Module : AElfModule<ExecutionPluginForAcs5Module>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}