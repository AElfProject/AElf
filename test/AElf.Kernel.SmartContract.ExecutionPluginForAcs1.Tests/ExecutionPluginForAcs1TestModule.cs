using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs1Module))]
    public class ExecutionPluginForAcs1TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
//            context.Services.AddAssemblyOf<ExecutionPluginForAcs1TestModule>();
        }
    }
}