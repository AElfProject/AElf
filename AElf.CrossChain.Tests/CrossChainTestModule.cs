using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(AbpEventBusModule),
        typeof(CrossChainAElfModule))]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }
    }
}