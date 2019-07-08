using AElf.Contracts.TestKit;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs5Module))]
    public class ExecutionPluginForAcs5TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}