using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(CoreKernelAElfModule),
        typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
        }
    }
}