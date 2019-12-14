using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.ReferendumAuth
{
    [DependsOn(typeof(ContractTestModule))]
    public class ReferendumAuthContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o =>
            {
                o.ContractDeploymentAuthorityRequired = false;
                o.TransactionExecutionCounterThreshold = -1;
            });
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
}