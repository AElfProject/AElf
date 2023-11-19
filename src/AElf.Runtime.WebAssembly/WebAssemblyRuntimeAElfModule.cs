using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.WebAssembly.Contract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly;

[DependsOn(
    typeof(WebAssemblyContractAElfModule),
    typeof(SmartContractAElfModule)
)]
public class WebAssemblyRuntimeAElfModule : AElfModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<WebAssemblyRuntimeOption>(configuration.GetSection("WebAssemblyRuntime"));
    }
}