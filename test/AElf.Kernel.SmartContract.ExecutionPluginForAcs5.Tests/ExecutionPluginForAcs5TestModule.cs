using AElf.Blockchains.MainChain;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs5Module))]
    public class ExecutionPluginForAcs5TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<ISystemTransactionMethodNameListProvider, SystemTransactionMethodNameListProvider>();
        }
    }
}