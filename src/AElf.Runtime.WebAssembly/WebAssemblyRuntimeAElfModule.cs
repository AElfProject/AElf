using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly;

[DependsOn(typeof(SmartContractAElfModule))]
public class WebAssemblyRuntimeAElfModule : AElfModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISmartContractRunner, WebAssemblySmartContractRunner>(_ =>
            new WebAssemblySmartContractRunner());
#if DEBUG
        context.Services.AddSingleton<ISmartContractRunner, UnitTestWebAssemblySmartContractRunner>(_ =>
            new UnitTestWebAssemblySmartContractRunner());
#endif
    }
}