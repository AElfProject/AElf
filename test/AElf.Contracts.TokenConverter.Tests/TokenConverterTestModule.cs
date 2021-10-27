using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AElf.Contracts.TokenConverter
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    public class TokenConverterTestModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddAssemblyOf<TokenConverterTestModule>();
        }
    }
}