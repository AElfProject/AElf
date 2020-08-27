using AElf.CrossChain.Indexing.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Indexing
{
    public class TransactionInputForBlockMiningDataProviderTests : CrossChainTestBase
    {
        private readonly ITransactionInputForBlockMiningDataProvider _transactionInputForBlockMiningDataProvider;

        public TransactionInputForBlockMiningDataProviderTests()
        {
            _transactionInputForBlockMiningDataProvider =
                GetRequiredService<ITransactionInputForBlockMiningDataProvider>();
        }

        [Fact]
        public void TransactionInputForBlockMiningDataProvider_AddAndGet_Tests()
        {
            var blockHash = HashHelper.ComputeFrom("Random");
            var crossChainTransactionInput = new CrossChainTransactionInput
            {
                PreviousBlockHeight = 10
            };
            _transactionInputForBlockMiningDataProvider.AddTransactionInputForBlockMining(blockHash,
                crossChainTransactionInput);

            {
                var actual = _transactionInputForBlockMiningDataProvider.GetTransactionInputForBlockMining(blockHash);
                actual.ShouldBe(crossChainTransactionInput);
            }

            {
                _transactionInputForBlockMiningDataProvider.ClearExpiredTransactionInput(11);
                var actual = _transactionInputForBlockMiningDataProvider.GetTransactionInputForBlockMining(blockHash);
                actual.ShouldBeNull();
            }
        }
    }
}