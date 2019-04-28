using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Election
{
    [DependsOn(typeof(ContractTestModule))]
    public class ElectionContractTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionExecutor, AElfConsensusTransactionExecutor>();
        }
    }
}