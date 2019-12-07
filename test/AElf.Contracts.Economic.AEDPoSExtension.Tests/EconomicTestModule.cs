using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    // ReSharper disable once InconsistentNaming
    public class EconomicTestModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<EconomicTestModule>();
            context.Services.AddSingleton<IChargeFeeStrategy, ConsensusContractChargeFeeStrategy>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}