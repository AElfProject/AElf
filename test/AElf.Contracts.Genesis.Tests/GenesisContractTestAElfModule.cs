using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class BasicContractZeroTestAElfModule : ContractTestAElfModule
    {
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var codeCheck = context.ServiceProvider.GetRequiredService<ICodeCheckService>();
            codeCheck.Enable();
        }
    }

    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class AuthorityNotRequiredBasicContractZeroTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}