using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.CrossChain.Tests
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    public class CrossChainContractTestAElfModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<CrossChainContractTestAElfModule>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}