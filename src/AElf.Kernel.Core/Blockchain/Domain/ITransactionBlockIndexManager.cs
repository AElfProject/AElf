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
            return await _transactionBlockIndexes.GetAsync(transactionId.ToStorageKey());
        }

        public async Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            await _transactionBlockIndexes.SetAsync(transactionId.ToStorageKey(), transactionBlockIndex);
        }
    }
}