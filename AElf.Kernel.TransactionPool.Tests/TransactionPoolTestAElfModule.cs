using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool.Tests
{
    [DependsOn(
        typeof(TransactionPoolAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class TransactionPoolTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}