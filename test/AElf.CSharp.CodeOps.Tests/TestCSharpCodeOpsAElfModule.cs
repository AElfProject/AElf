using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Volo.Abp.Modularity;

namespace AElf.CSharp.CodeOps
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(SmartContractTestAElfModule)
    )]
    public class TestCSharpCodeOpsAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
