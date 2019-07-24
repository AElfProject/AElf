using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
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
            Block = OsTestHelper.GenerateBlock(Block.GetHash(), Block.Height,
                SystemTransactions.Concat(CancellableTransactions));

            var block = await BlockExecutingService.ExecuteBlockAsync(Block.Header,
                SystemTransactions, CancellableTransactions, CancellationToken.None);
            block.TransactionIds.Count().ShouldBeGreaterThan(10);
        }

        [Fact]
        public async Task GetTransactionParametersAsync_Test()
        {
            var chain = await BlockchainService.GetChainAsync();
            var context = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var transaction = await OsTestHelper.GenerateTransferTransaction();

            var jsonResult =
                await TransactionReadOnlyExecutionService.GetTransactionParametersAsync(context, transaction);

            jsonResult.ShouldNotBeEmpty();
            jsonResult.ShouldContain("to");
            jsonResult.ShouldContain("symbol");
            jsonResult.ShouldContain("amount");
        }

        [Fact]
        public async Task TransactionReadOnlyExecutionServiceExtensions_Test()
        {
            var chain = await BlockchainService.GetChainAsync();
            var context = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var transaction = OsTestHelper.GenerateTransaction(Address.Generate(),
                SmartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                "GetBalance", new GetBalanceInput
                {
                    Owner = Address.Generate(),
                    Symbol = "ELF"
                });

            var result = await TransactionReadOnlyExecutionService.ExecuteAsync<GetBalanceOutput>(context, transaction,
                TimestampHelper.GetUtcNow(),
                true);
            result.Balance.ShouldBe(0);
            result.Symbol.ShouldBe("ELF");

            //without such method and call
            transaction.MethodName = "NotExist";
            await Should.ThrowAsync<SmartContractExecutingException>(async () =>
            {
                await TransactionReadOnlyExecutionService.ExecuteAsync<GetBalanceOutput>(context, transaction,
                    TimestampHelper.GetUtcNow(),
                    true);
            });
        }
    }
}