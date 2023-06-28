using AElf.ContractTestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Economic.TestBase;

[DependsOn(typeof(ContractTestModule))]
public class EconomicContractsTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);

        context.Services.AddSingleton<ITestTransactionExecutor, EconomicTestTransactionExecutor>();
        context.Services.AddSingleton<ITriggerInformationProvider, AEDPoSTriggerInformationProvider>();
        context.Services.AddSingleton<IBlockValidationService, MockBlockValidationService>();
        // context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
        // context.Services.AddSingleton<IPreExecutionPlugin, MethodCallingThresholdPreExecutionPlugin>();
        // context.Services.AddSingleton<IPreExecutionPlugin, ResourceConsumptionPreExecutionPlugin>();
        // context.Services.AddSingleton<IPostExecutionPlugin, ResourceConsumptionPostExecutionPlugin>();
        context.Services.AddSingleton<ISecretSharingService, SecretSharingService>();
        context.Services.AddSingleton<IInValueCache, InValueCache>();
        context.Services.AddTransient<IRandomNumberProvider, MockRandomNumberProvider>();
        context.Services.RemoveAll<IPreExecutionPlugin>();
    }
}