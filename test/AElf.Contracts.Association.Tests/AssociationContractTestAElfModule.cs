using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Association
{
    [DependsOn(typeof(ContractTestModule))]
    public class AssociationContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }

    [DependsOn(typeof(AssociationContractTestAElfModule))]
    public class AssociationContractTestAElfModuleWithSpecificChainId : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(options => options.ChainId = ChainId + 1);
        }
    }
}