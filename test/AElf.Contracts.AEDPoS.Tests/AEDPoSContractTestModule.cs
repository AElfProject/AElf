using AElf.ContractTestBase;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AEDPoS
{
    [DependsOn(typeof(MainChainContractTestModule))]
    // ReSharper disable once InconsistentNaming
    public class AEDPoSContractTestAElfModule : MainChainContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.RemoveAll<IPostExecutionPlugin>();
            context.Services.RemoveAll<ISystemTransactionGenerator>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}