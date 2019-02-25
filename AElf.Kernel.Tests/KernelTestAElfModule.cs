using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
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

            services.AddTransient<BlockValidationProvider>();

            services.AddTransient<IBlockExecutingService>(p =>
            {
                var mockBlockExecutingService = new Mock<IBlockExecutingService>();
                mockBlockExecutingService.Setup(m => m.ExecuteBlockAsync(It.IsAny<int>(), It.IsAny<BlockHeader>(),
                        It.IsAny<IEnumerable<Transaction>>()))
                    .Returns<int, BlockHeader, IEnumerable<Transaction>>(
                        (chainId, blockHeader, nonCancellableTransactions)
                            => Task.FromResult(new Block {Header = blockHeader}));
                return mockBlockExecutingService.Object;
            });
            
            services.AddTransient<IBlockValidationService>(p =>
            {
                var mockBlockValidationService = new Mock<IBlockValidationService>();
                mockBlockValidationService
                    .Setup(m => m.ValidateBlockBeforeExecuteAsync(It.IsAny<int>(), It.IsAny<Block>()))
                    .Returns<int, Block>((chainId, block) =>
                        Task.FromResult(block?.Header != null && block.Body != null));
                mockBlockValidationService
                    .Setup(m => m.ValidateBlockAfterExecuteAsync(It.IsAny<int>(), It.IsAny<Block>()))
                    .Returns<int, Block>((chainId, block) => Task.FromResult(true));
                return mockBlockValidationService.Object;
            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
        }
    }
}