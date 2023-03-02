using AElf.CSharp.CodeOps;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp;

[DependsOn(
    typeof(CSharpRuntimeAElfModule),
    typeof(SmartContractTestAElfModule),
    typeof(CSharpCodeOpsAElfModule)
)]
public class CSharpRuntimeTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient(p => Mock.Of<ICodePatchService>());
    }
}