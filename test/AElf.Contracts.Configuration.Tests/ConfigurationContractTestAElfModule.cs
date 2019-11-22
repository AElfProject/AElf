using AElf.Contracts.TestBase;
using AElf.Kernel.Miner.Application;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Contracts.ConfigurationContract.Tests
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class ConfigurationContractTestAElfModule: ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(provider =>
            {
                var mockService = new Mock<IBlockTransactionLimitProvider>();
                mockService.Setup(m => m.InitAsync());
                return mockService.Object;
            });
        }
    }
}