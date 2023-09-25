using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        var services = context.Services;
    }
}

[DependsOn(
    typeof(WebAssemblyRuntimeAElfModule),
    typeof(SmartContractTestAElfModule)
)]
public class WebAssemblyRuntimeMockedTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        //services.AddSingleton<IExternalEnvironment, UnitTestExternalEnvironment>();
        context.Services.RemoveAll<ISmartContractRunner>();
        context.Services.AddSingleton<ISmartContractRunner, WebAssemblySmartContractRunner>(_ =>
            new WebAssemblySmartContractRunner(new UnitTestExternalEnvironment()));
    }
}
