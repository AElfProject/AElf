using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [DependsOn(typeof(ContractTestModule))]
    public class AElfConsensusContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionExecutor, AElfConsensusTransactionExecutor>();
        }
    }
}