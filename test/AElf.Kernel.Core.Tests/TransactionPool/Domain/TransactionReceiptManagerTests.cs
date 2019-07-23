using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Domain
{
    public sealed class TransactionReceiptManagerTests:AElfKernelTestBase
    {
        private ITransactionReceiptManager _transactionReceiptManager;

        public TransactionReceiptManagerTests()
        {
            _transactionReceiptManager = GetRequiredService<TransactionReceiptManager>();
        }

        [Fact]
        public async Task AddOrUpdateReceipt_Test()
        {
            var transactionReceipt = GenerateTransactionReceipts(1)[0];
            await _transactionReceiptManager.AddOrUpdateReceiptAsync(transactionReceipt);
            var result = await _transactionReceiptManager.GetReceiptAsync(transactionReceipt.TransactionId);
            result.ShouldBe(transactionReceipt);

            transactionReceipt.Transaction.MethodName = "TestUpdate";
            await _transactionReceiptManager.AddOrUpdateReceiptAsync(transactionReceipt);
            result = await _transactionReceiptManager.GetReceiptAsync(transactionReceipt.TransactionId);
            result.Transaction.MethodName.ShouldBe("TestUpdate");
        }

        [Fact]
        public async Task AddOrUpdateReceipts_Test()
        {
            var transactionReceipts = GenerateTransactionReceipts(3);
            await _transactionReceiptManager.AddOrUpdateReceiptsAsync(transactionReceipts);

            var result = await _transactionReceiptManager.GetReceiptAsync(transactionReceipts[0].TransactionId);
            result.ShouldBe(transactionReceipts[0]);

            //update
            foreach (var transactionReceipt in transactionReceipts)
            {
                transactionReceipt.Transaction.MethodName = "UpdateMethod";
            }

            await _transactionReceiptManager.AddOrUpdateReceiptsAsync(transactionReceipts);
            var result1 = await _transactionReceiptManager.GetReceiptAsync(transactionReceipts[0].TransactionId);
            result1.Transaction.MethodName.ShouldBe("UpdateMethod");
        }

        [Fact]
        public async Task GetReceipt_Test()
        {
            var randomHash = Hash.FromRawBytes(new byte[]{1,2});
            var transactionReceipt0 = await _transactionReceiptManager.GetReceiptAsync(randomHash);
            transactionReceipt0.ShouldBe(null);

            var transactionReceipt = GenerateTransactionReceipts(1)[0];

            await _transactionReceiptManager.AddOrUpdateReceiptAsync(transactionReceipt);
            var result = await _transactionReceiptManager.GetReceiptAsync(transactionReceipt.TransactionId);

            result.ShouldBe(transactionReceipt);
        }
        
        private List<TransactionReceipt> GenerateTransactionReceipts(int count)
        {
            var transactionReceipts = new List<TransactionReceipt>();
            for (int i = 0; i < count; i++)
            {
                var transactionId = Hash.FromRawBytes(new []{Convert.ToByte(i)});
                var transactionReceipt = new TransactionReceipt()
                {
                    TransactionId = transactionId,
                    Transaction = new Transaction()
                    {
                        From = AddressHelper.StringToAddress("from"),
                        To = AddressHelper.StringToAddress("to"),
                        MethodName = "TestMethod"
                    },
                    ExecutedBlockNumber = i,
                    IsSystemTxn = false,
                    RefBlockStatus = RefBlockStatus.RefBlockValid
                };
                transactionReceipts.Add(transactionReceipt);
            }

            return transactionReceipts;
        }
    }
}