using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(BlockTransactionLimitControllerModule))]
    public class TestModule : ContractTestModule<TestModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TestModule>();
        }
    }
}