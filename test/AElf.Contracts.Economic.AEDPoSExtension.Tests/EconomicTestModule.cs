using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    // ReSharper disable once InconsistentNaming
    public class EconomicTestModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<EconomicTestModule>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}