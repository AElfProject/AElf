using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.TransactionPool.Domain
{
    public class TransactionReceiptManager : ITransactionReceiptManager
    {
        private readonly IBlockchainStore<TransactionReceipt> _transactionReceiptStore;

        public TransactionReceiptManager(IBlockchainStore<TransactionReceipt> transactionReceiptStore)
        {
            _transactionReceiptStore = transactionReceiptStore;
        }

        public async Task AddOrUpdateReceiptAsync(TransactionReceipt receipt)
        {
            await _transactionReceiptStore.SetAsync(receipt.TransactionId.ToStorageKey(), receipt);
        }

        public async Task AddOrUpdateReceiptsAsync(IEnumerable<TransactionReceipt> receipts)
        {
            var dict = receipts.ToDictionary(r => r.TransactionId.ToStorageKey(), r => r);
            await _transactionReceiptStore.PipelineSetAsync(dict);
        }

        public async Task<TransactionReceipt> GetReceiptAsync(Hash txId)
        {
            var result = await _transactionReceiptStore.GetAsync(txId.ToStorageKey());
            return result;
        }
    }
}