using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly.Tests;

[DependsOn(
    typeof(WebAssemblyRuntimeAElfModule),
    typeof(SmartContractTestAElfModule)
)]
public class WebAssemblyRuntimeTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}