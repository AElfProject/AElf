using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Runtime.WebAssembly.TransactionPayment;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly;

[DependsOn(
    typeof(SmartContractAElfModule),
    typeof(CSharpRuntimeAElfModule)
)]
public class WebAssemblyRuntimeAElfModule : AElfModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}