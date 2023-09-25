using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly;
using AElf.Runtime.WebAssembly.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.SolidityContract;

[DependsOn(typeof(ContractTestModule),
    typeof(SmartContractAElfModule),
    typeof(WebAssemblyRuntimeAElfModule))]
public class SolidityContractTestAElfModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.RemoveAll<IPreExecutionPlugin>();
    }
}