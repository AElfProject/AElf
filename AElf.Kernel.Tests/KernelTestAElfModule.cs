using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    [DependsOn(typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            //init test data here
        }
    }
}