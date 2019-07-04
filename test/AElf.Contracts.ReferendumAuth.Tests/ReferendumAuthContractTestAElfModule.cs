using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.ReferendumAuth
{
    [DependsOn(typeof(ContractTestModule))]
    public class ReferendumAuthContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.AddSingleton<ITransactionExecutor, ParliamentAuthContractTransactionExecutor>();
        }
    }
}