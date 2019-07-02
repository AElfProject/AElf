using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs5;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.EconomicSystem.Tests
{
    [DependsOn(typeof(ContractTestModule))]
    public class EconomicSystemTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // Justification: Some test cases need to mock current block time.
            context.Services.AddSingleton<ITransactionExecutor, EconomicTransactionExecutor>();
            context.Services.AddSingleton<IBlockValidationService, MockBlockValidationService>();
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<IPreExecutionPlugin, MethodCallingThresholdPreExecutionPlugin>();
            context.Services.AddSingleton<IPreExecutionPlugin, ResourceConsumptionPreExecutionPlugin>();
            context.Services.AddSingleton<IPostExecutionPlugin, ResourceConsumptionPostExecutionPlugin>();
        }
    }
}