using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionBlockIndexManager
    {
        Task<TransactionBlockIndex> GetTransactionBlockIndexAsync(Hash transactionId);
        Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex);
        Task SetTransactionBlockIndicesAsync(IDictionary<Hash, TransactionBlockIndex> transactionBlockIndexes);
        Task RemoveTransactionIndicesAsync(IEnumerable<Hash> txIds);
    }

    public class TransactionBlockIndexManager : ITransactionBlockIndexManager
    {
        private readonly IBlockchainStore<TransactionBlockIndex> _transactionBlockIndexes;

        public TransactionBlockIndexManager(IBlockchainStore<TransactionBlockIndex> transactionBlockIndexes)
        {
            _transactionBlockIndexes = transactionBlockIndexes;
        }

        public async Task<TransactionBlockIndex> GetTransactionBlockIndexAsync(Hash transactionId)
        {
            var transactionBlockIndex = await _transactionBlockIndexes.GetAsync(transactionId.ToStorageKey());
            return transactionBlockIndex;
        }

        public async Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            await _transactionBlockIndexes.SetAsync(transactionId.ToStorageKey(), transactionBlockIndex);
        }
        
        public async Task SetTransactionBlockIndicesAsync(IDictionary<Hash,TransactionBlockIndex> transactionBlockIndexes)
        {
            await _transactionBlockIndexes.SetAllAsync(
                transactionBlockIndexes.ToDictionary(t => t.Key.ToStorageKey(), t => t.Value));
        }

        public async Task RemoveTransactionIndicesAsync(IEnumerable<Hash> txIds)
        {
            await _transactionBlockIndexes.RemoveAllAsync(txIds.Select(txId => txId.ToStorageKey()).ToList());
        }
    }
}