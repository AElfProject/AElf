using AElf.ContractDeployer;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase.Tests
{
    [DependsOn(
        typeof(MainChainContractTestModule)
    )]
    public class MainChainTestModule : MainChainContractTestModule
    {
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<MainChainTestModule>();
        }
    }
}