using AElf.ContractDeployer;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

[DependsOn(typeof(ContractTestModule))]
public class VirtualTransactionEventContractsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddTransient<IContractDeploymentListProvider, ContractDeploymentListProvider>();
    }
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
        contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<VirtualTransactionEventContractsTestModule>();
    }
}