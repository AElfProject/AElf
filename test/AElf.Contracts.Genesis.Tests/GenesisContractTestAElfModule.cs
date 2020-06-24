using AElf.Contracts.TestBase;
using AElf.ContractTestKit;
using AElf.Kernel.CodeCheck;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            var codeCheck = context.ServiceProvider.GetRequiredService<IOptionsMonitor<CodeCheckOptions>>();
            codeCheck.CurrentValue.CodeCheckEnabled = true;
        }
    }

    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class AuthorityNotRequiredBasicContractZeroTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
        }
    }
}