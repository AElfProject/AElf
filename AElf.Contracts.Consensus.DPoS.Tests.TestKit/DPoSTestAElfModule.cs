using AElf.Contracts.TestKit;
using AElf.Kernel.Account.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.DPoS
{
    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class DPoSTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionExecutor, DPoSTransactionExecutor>();
            context.Services.AddSingleton<IECKeyPairProvider, ECKeyPairProvider>();
            context.Services.AddSingleton<IAccountService, MockAccountService>();
        }
    }
}