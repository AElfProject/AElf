using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [DependsOn(typeof(ContractTestModule))]
    public class AEDPoSContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionExecutor, AEDPoSContractTransactionExecutor>();
            context.Services.AddSingleton<ITriggerInformationProvider, AEDPoSTriggerInformationProvider>();
            context.Services.AddSingleton<IRandomHashCacheService, MockRandomHashCacheService>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}