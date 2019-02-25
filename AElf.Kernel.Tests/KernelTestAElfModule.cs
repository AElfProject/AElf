using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(CoreKernelAElfModule),
        typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

            var mockBlockExecutingService = new Mock<IBlockExecutingService>();
            mockBlockExecutingService.Setup(m => m.ExecuteBlockAsync(It.IsAny<int>(), It.IsAny<BlockHeader>(),
                    It.IsAny<IEnumerable<Transaction>>()))
                .Returns<int, BlockHeader, IEnumerable<Transaction>>((chainId, blockHeader, nonCancellableTransactions)
                    => Task.FromResult(new Block {Header = blockHeader}));

            services.AddTransient<IBlockExecutingService>(p => mockBlockExecutingService.Object);
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
        }
    }
}