using AElf.Contracts.TestKit;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
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
            context.Services.AddSingleton<IAccountService, AccountService>();
        }
    }
}