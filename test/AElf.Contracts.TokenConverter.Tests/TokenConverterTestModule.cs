using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AElf.Contracts.TokenConverter
{
    [DependsOn(typeof(ContractTestModule))]
    public class TokenConverterTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
}