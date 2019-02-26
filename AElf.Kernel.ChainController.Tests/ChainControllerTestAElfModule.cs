using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(CoreKernelAElfModule),
        typeof(TestBaseAElfModule))]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

            services.AddTransient<IBlockExecutingService>(p =>
            {
                var mockBlockExecutingService = new Mock<IBlockExecutingService>();
                mockBlockExecutingService.Setup(m => m.ExecuteBlockAsync(It.IsAny<int>(), It.IsAny<BlockHeader>(),
                        It.IsAny<IEnumerable<Transaction>>()))
                    .Returns<int, BlockHeader, IEnumerable<Transaction>>(
                        (chainId, blockHeader, nonCancellableTransactions)
                            => Task.FromResult(new Block {Header = blockHeader,Body = new BlockBody()}));
                return mockBlockExecutingService.Object;
            });
            
            services.AddTransient<ChainCreationService>();
        }
    }
}