using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs8Module))]
    public class ExecutionPluginForAcs8TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}