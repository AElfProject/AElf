using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool.Tests
{
    [DependsOn(typeof(CoreKernelAElfModule),
        typeof(TestBaseAElfModule))]
    public class TransactionPoolAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }
    }
}