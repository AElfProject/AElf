using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class ParallelTransactionExecutionTests : ParallelExecutionTestBase
    {
        [Fact]
        public async Task ParallelExecuteAsync_Test()
        {
            var chain = await BlockchainService.GetChainAsync();

            (PrepareTransactions, KeyPairs) = await OsTestHelper.PrepareTokenForParallel(10);
            Block = OsTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, PrepareTransactions);
            await BlockExecutingService.ExecuteBlockAsync(Block.Header, PrepareTransactions);
            await OsTestHelper.BroadcastTransactions(PrepareTransactions);
            Block = await MinerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromSeconds(4));
            
            SystemTransactions = await OsTestHelper.GenerateTransferTransactions(1);
            CancellableTransactions = await OsTestHelper.GenerateTransactionsWithoutConflict(KeyPairs);
            chain = await BlockchainService.GetChainAsync();
            Block = OsTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
                SystemTransactions.Concat(CancellableTransactions));
            
            var block = await BlockExecutingService.ExecuteBlockAsync(Block.Header, 
                SystemTransactions, CancellableTransactions, CancellationToken.None);
            block.TransactionIds.Count().ShouldBeGreaterThan(10);
        }
    }
}