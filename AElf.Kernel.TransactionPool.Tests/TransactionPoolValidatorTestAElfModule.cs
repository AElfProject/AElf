using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool.Tests
{
    [DependsOn(
        typeof(TransactionPoolTestAElfModule)
    )]
    public class TransactionPoolValidatorTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // TODO: TxRefBlockValidator is still in use or not?
            context.Services.AddSingleton<IBlockchainService>(o =>
            {
                var chain = new Mock<IBlockchainService>();
                chain.Setup(x => x.GetChainAsync()).Returns(Task.FromResult<Chain>(new Chain()
                {
                    BestChainHeight = 100,
                }));
                chain.Setup(x => x.GetBlockHashByHeightAsync(It.IsAny<Chain>(), 80u, null))
                    .Returns(Task.FromResult<Hash>(Hash.FromString("test")));
                return chain.Object;
            });
            context.Services.AddSingleton<ITxRefBlockValidator, TxRefBlockValidator>();
        }
    }
}