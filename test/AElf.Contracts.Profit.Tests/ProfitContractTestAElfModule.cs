using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Profit
{
    [DependsOn(typeof(ContractTestModule))]
    public class ProfitContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
        }
    }
}