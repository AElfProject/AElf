using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(SmartContractTestAElfModule)
    )]
    public class TestCSharpRuntimeAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}