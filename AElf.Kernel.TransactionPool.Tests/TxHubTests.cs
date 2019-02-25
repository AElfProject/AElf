using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.TransactionPool.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Tests
{
    public class TxHubTests:TransactionPoolTestBase
    {
        private const int ChainId = 1234;
        private TxHub _txHub;

        public TxHubTests()
        {
            _txHub = GetRequiredService<TxHub>();
        }

        [Fact]
        public void Add_Transaction()
        {
            //Skip validation
            var transaction = FakeTransaction.Generate();
            var result = _txHub.AddTransactionAsync(ChainId, transaction, true).Result;
            result.ShouldBe(true);

            //Not skiipi validation
            var transaction1 = FakeTransaction.Generate();
            var result1 = _txHub.AddTransactionAsync(ChainId, transaction1).Result;
            result1.ShouldBe(true);

            //Add again
            var result2= _txHub.AddTransactionAsync(ChainId, transaction1).Result;
            result2.ShouldBe(false);
        }

        [Fact]
        public void Query_Or_Create_CheckedReceipts()
        {
            var transaction = FakeTransaction.Generate();
            _txHub.AddTransactionAsync(ChainId, transaction, true).Result.ShouldBe(true);
            //Exist
            var transactionReceipt = _txHub.GetCheckedReceiptsAsync(ChainId, transaction).Result;
            transactionReceipt.Transaction.ShouldBe(transaction);

            //Not Exist
            var transaction1 = FakeTransaction.Generate();
            var transactionReceipt1 = _txHub.GetCheckedReceiptsAsync(ChainId, transaction1).Result;
            transactionReceipt1.Transaction.ShouldBe(transaction1);
            transactionReceipt1.SignatureStatus.ShouldBe(SignatureStatus.SignatureInvalid);
            transactionReceipt1.RefBlockStatus.ShouldBe(RefBlockStatus.RefBlockValid);
        }

        [Fact]
        public void Get_Executable_Transactions()
        {
            var transaction1 = FakeTransaction.Generate();
            var transaction2 = FakeTransaction.Generate();
            _txHub.AddTransactionAsync(ChainId, transaction1).Result.ShouldBe(true);
            _txHub.AddTransactionAsync(ChainId, transaction2, true).Result.ShouldBe(true);

            var executableTxs = _txHub.GetReceiptsOfExecutablesAsync().Result;
            executableTxs.Count.ShouldBe(1);
            executableTxs[0].Transaction.ShouldBe(transaction2);
        }

        [Fact]
        public void Get_Transaction_And_Receipt()
        {
            var transaction = FakeTransaction.Generate();
            _txHub.AddTransactionAsync(ChainId, transaction, true).Result.ShouldBe(true);

            var transactionReceipt = _txHub.GetReceiptAsync(transaction.GetHash()).Result;
            transactionReceipt.Transaction.ShouldBe(transaction);
            transactionReceipt.TransactionId = transaction.GetHash();

            var transaction1 = _txHub.GetTxAsync(transaction.GetHash()).Result;
            transaction1.ShouldBe(transaction);
        }
    }
}