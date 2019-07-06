using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AEDPoSTestBase
{
    [DependsOn(typeof(ContractTestModule))]
    public class AEDPoSTestBaseModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionListProvider, TransactionListProvider>();
            context.Services.AddSingleton<IPostExecutionPlugin, PostExecutionPlugin>();
        }
    }
}