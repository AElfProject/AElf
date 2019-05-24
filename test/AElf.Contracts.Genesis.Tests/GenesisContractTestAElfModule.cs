using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class BasicContractZeroTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<BasicContractZeroTestAElfModule>(); 
        }
    }

    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class AuthorityNotRequiredBasicContractZeroTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AuthorityNotRequiredBasicContractZeroTestModule>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}