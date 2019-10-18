using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8;
using Volo.Abp.Modularity;

namespace AElf.Contract.TestContract
{
    [DependsOn(typeof(ContractTestModule))]
    public class TestContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }

    [DependsOn(
        typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs1Module),
        typeof(ExecutionPluginForAcs8Module))]
    public class TestFeesContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        } 
    }
}