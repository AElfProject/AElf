using AElf.Kernel;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSAElfModule), typeof(KernelTestAElfModule))]
    public class SyncStateTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}