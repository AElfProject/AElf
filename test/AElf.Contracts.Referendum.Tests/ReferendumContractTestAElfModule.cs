using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Referendum
{
    [DependsOn(typeof(ContractTestModule))]
    public class ReferendumContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
}