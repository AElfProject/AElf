using AElf.Contracts.TestKit;
using AElf.Kernel.Miner.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
    [DependsOn(typeof(ContractTestModule))]
    public class MultiTokenContractTestAElfModule : ContractTestModule<MultiTokenContractTestAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<MultiTokenContractTestAElfModule>();
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
        }
    }
}