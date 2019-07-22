using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(BlockTransactionLimitControllerModule))]
    public class TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}