using AElf.ContractTestKit;
using AElf.Contracts.TestBase;
using AElf.GovernmentSystem;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Parliament
{
    [DependsOn(typeof(ContractTestModule),
        typeof(GovernmentSystemAElfModule))]
    public class ParliamentContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
    
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class ParliamentContractPrivilegeTestAElfModule : ContractTestAElfModule
    {
    }
}