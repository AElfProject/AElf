using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public sealed class LocalParallelTransactionExecutingServiceTests : ParallelExecutionTestBase
    {
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        public LocalParallelTransactionExecutingServiceTests()
        {
            _transactionExecutingService = GetRequiredService<ITransactionExecutingService>();
            _systemTransactionExtraDataProvider = GetRequiredService<ISystemTransactionExtraDataProvider>();
        }

        [Fact]
        public async Task ExecuteAsync_Test()
        {
            var chain = await BlockchainService.GetChainAsync();

            (PrepareTransactions, KeyPairs) = await OsTestHelper.PrepareTokenForParallel(10);
            Block = OsTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, PrepareTransactions);
            PrepareTransactions[0].To = SampleAccount.Accounts[0].Address;
            await OsTestHelper.BroadcastTransactions(PrepareTransactions);
            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(new TransactionExecutingDto
            {
                Transactions = PrepareTransactions,
                BlockHeader = Block.Header
            }, CancellationToken.None);
            executionReturnSets.Count.ShouldBe(PrepareTransactions.Count);
            executionReturnSets.Count(set => set.TransactionResult.Status == TransactionResultStatus.Failed)
                .ShouldBe(1);
            executionReturnSets.First(set => set.TransactionResult.Status == TransactionResultStatus.Failed).TransactionResult.Error.ShouldBe("Invalid contract address.");
            executionReturnSets.Count(set => set.TransactionResult.Status == TransactionResultStatus.Mined).ShouldBe(9);
            
            (PrepareTransactions, KeyPairs) = await OsTestHelper.PrepareTokenForParallel(10);
            Block = OsTestHelper.GenerateBlock(Block.GetHash(), Block.Height, PrepareTransactions);
            await OsTestHelper.BroadcastTransactions(PrepareTransactions);
            var cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.Cancel();
            executionReturnSets = await _transactionExecutingService.ExecuteAsync(new TransactionExecutingDto
            {
                Transactions = PrepareTransactions,
                BlockHeader = Block.Header
            }, cancelTokenSource.Token);
            executionReturnSets.Count.ShouldBe(0);
            
            (PrepareTransactions, KeyPairs) = await OsTestHelper.PrepareTokenForParallel(10);
            Block = OsTestHelper.GenerateBlock(Block.GetHash(), Block.Height, PrepareTransactions);
            _systemTransactionExtraDataProvider.SetSystemTransactionCount(1,Block.Header);
            executionReturnSets = await _transactionExecutingService.ExecuteAsync(new TransactionExecutingDto
            {
                Transactions = PrepareTransactions,
                BlockHeader = Block.Header
            }, CancellationToken.None);
            executionReturnSets.Count.ShouldBe(PrepareTransactions.Count);
            executionReturnSets.ShouldAllBe(set => set.TransactionResult.Status == TransactionResultStatus.Mined);
        }
    }
}