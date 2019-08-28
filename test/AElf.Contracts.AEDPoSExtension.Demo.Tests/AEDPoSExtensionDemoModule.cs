using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtensionDemoModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AEDPoSExtensionDemoModule>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}