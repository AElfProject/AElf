using AElf.Contracts.TestKit;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
    [DependsOn(typeof(ContractTestModule))]
    public class MultiTokenContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}