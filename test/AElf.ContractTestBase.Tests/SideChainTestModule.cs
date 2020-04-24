using AElf.Contracts.Deployer;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase.Tests
{
    [DependsOn(
        typeof(SideChainContractTestModule)
    )]
    public class SideChainTestModule : SideChainContractTestModule
    {
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<SideChainTestModule>();
        }
    }
}