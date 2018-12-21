using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Manager.Managers
{
    public class TransactionReceiptManager : ITransactionReceiptManager
    {
        private readonly ITransactionReceiptStore _transactionReceiptStore;

        public TransactionReceiptManager(ITransactionReceiptStore transactionReceiptStore)
        {
            _transactionReceiptStore = transactionReceiptStore;
        }

        public async Task AddOrUpdateReceiptAsync(TransactionReceipt receipt)
        {
            await _transactionReceiptStore.SetAsync(receipt.TransactionId.DumpHex(), receipt);
        }

        public async Task AddOrUpdateReceiptsAsync(IEnumerable<TransactionReceipt> receipts)
        {
            var dict = receipts.ToDictionary(r => r.TransactionId.DumpHex(), r => (object)r);
            await _transactionReceiptStore.PipelineSetAsync(dict);
        }

        public async Task<TransactionReceipt> GetReceiptAsync(Hash txId)
        {
            var result = await _transactionReceiptStore.GetAsync<TransactionReceipt>(txId.DumpHex());
            return result;
        }
    }
}