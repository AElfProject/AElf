using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            context.Services.AddSingleton<IBlockchainService>(o =>
            {
                var chain = new Mock<IBlockchainService>();
                chain.Setup(x => x.GetChainAsync(It.IsAny<int>())).Returns(Task.FromResult<Chain>(new Chain()
                {
                    BestChainHeight = 100,
                }));
                chain.Setup(x => x.GetBlockHashByHeightAsync(It.IsAny<Chain>(), 80u, null))
                    .Returns(Task.FromResult<Hash>(Hash.FromString("test")));
                return chain.Object;
            });
            context.Services.AddSingleton<ITxRefBlockValidator, TxRefBlockValidator>();
            context.Services.AddSingleton<TxHub>();
        }
    }
}