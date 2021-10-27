using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
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
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IBlockValidationProvider, ConsensusValidationProvider>();
            context.Services.AddSingleton<IBlockValidationService, BlockValidationService>();
        }
    }
}