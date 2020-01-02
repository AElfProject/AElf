using AElf.Contracts.Economic.TestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Election
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class ElectionContractTestModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
}