/*using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CSharp.CodeOps;

[DependsOn(
    typeof(CSharpRuntimeAElfModule),
    typeof(SmartContractTestAElfModule),
    typeof(CSharpCodeOpsAElfModule)
)]
public class TestCSharpCodeOpsAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<CSharpContractAuditor>();
    }
}*/