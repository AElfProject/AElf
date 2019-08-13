using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForConstrainedTransaction
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForConstrainedTransactionModule : AElfModule<ExecutionPluginForConstrainedTransactionModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}