using AElf.Contracts.TestKit;
using AElf.Kernel.Account.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    [DependsOn(typeof(ContractTestModule))]
    public class AElfConsensusContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AElfConsensusContractTestAElfModule>();
            
            context.Services.AddSingleton<ITransactionExecutor, AElfConsensusTransactionExecutor>();
            context.Services.AddSingleton<IECKeyPairProvider, ECKeyPairProvider>();
            context.Services.AddSingleton<IAccountService, MockAccountService>();
        }
    }
}