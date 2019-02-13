using AElf.Kernel.Managers;
using System.Threading.Tasks;
using AElf.Common;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests.Managers
{
    public sealed class TransactionReceiptManagerTests:AElfKernelTestBase
    {
        private ITransactionReceiptManager _transactionReceiptManager;

        public TransactionReceiptManagerTests()
        {
            _transactionReceiptManager = GetRequiredService<TransactionReceiptManager>();
        }

        [Fact]
        public async Task AddOrUpdateReceiptTest()
        {
            var transactionId = Hash.Generate();
            var transactionReceipt = new TransactionReceipt()
            {
                TransactionId = transactionId,
                Transaction = new Transaction(),
                ExecutedBlockNumber = 1,
                IsSystemTxn = false,
                RefBlockStatus = RefBlockStatus.RefBlockValid
            };

            await _transactionReceiptManager.AddOrUpdateReceiptAsync(transactionReceipt);
            var result = await _transactionReceiptManager.GetReceiptAsync(transactionId);

            result.ShouldBe(transactionReceipt);
        }

        [Fact]
        public async Task AddOrUpdateReceiptsTest()
        {
        }

        [Fact]
        public async Task GetReceiptTest()
        {
        }
    }
}