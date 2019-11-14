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
        Task<TransactionBlockIndex> GetCachedTransactionBlockIndexAsync(Hash transactionId);
        Task CleanTransactionBlockIndexCacheAsync(long blockHeight);
    }

    public class TransactionBlockIndexManager : ITransactionBlockIndexManager
    {
        private readonly IBlockchainStore<TransactionBlockIndex> _transactionBlockIndexes;
        private readonly ITransactionBlockIndexCacheProvider _transactionBlockIndexCacheProvider;

        public TransactionBlockIndexManager(IBlockchainStore<TransactionBlockIndex> transactionBlockIndexes,
            ITransactionBlockIndexCacheProvider transactionBlockIndexCacheProvider)
        {
            _transactionBlockIndexes = transactionBlockIndexes;
            _transactionBlockIndexCacheProvider = transactionBlockIndexCacheProvider;
        }

        public async Task<TransactionBlockIndex> GetTransactionBlockIndexAsync(Hash transactionId)
        {
            if (!_transactionBlockIndexCacheProvider.TryGetValue(transactionId, out var transactionBlockIndex))
            {
                transactionBlockIndex = await _transactionBlockIndexes.GetAsync(transactionId.ToStorageKey());
                _transactionBlockIndexCacheProvider.AddOrUpdate(transactionId, transactionBlockIndex);
            }

            return transactionBlockIndex;
        }

        public async Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            _transactionBlockIndexCacheProvider.AddOrUpdate(transactionId, transactionBlockIndex);
            await _transactionBlockIndexes.SetAsync(transactionId.ToStorageKey(), transactionBlockIndex);
        }

        public Task<TransactionBlockIndex> GetCachedTransactionBlockIndexAsync(Hash transactionId)
        {
            _transactionBlockIndexCacheProvider.TryGetValue(transactionId, out var transactionBlockIndex);
            return Task.FromResult(transactionBlockIndex);
        }

        public Task CleanTransactionBlockIndexCacheAsync(long blockHeight)
        {
            _transactionBlockIndexCacheProvider.CleanByHeight(blockHeight);
            return Task.CompletedTask;
        }
    }
}