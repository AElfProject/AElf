using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult transactionResult, Hash disambiguationHash);
        Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults, Hash disambiguationHash);
        Task RemoveTransactionResultAsync(Hash txId, Hash disambiguationHash);
        Task RemoveTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash);
        Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash);
        Task<bool> HasTransactionResultsAsync(Hash transactionId, Hash disambiguationHash);
    }

    public class TransactionResultManager : ITransactionResultManager
    {
        private readonly IBlockchainStore<TransactionResult> _transactionResultStore;

        public TransactionResultManager(IBlockchainStore<TransactionResult> transactionResultStore)
        {
            _transactionResultStore = transactionResultStore;
        }

        public async Task AddTransactionResultAsync(TransactionResult transactionResult, Hash disambiguationHash)
        {
            await _transactionResultStore.SetAsync(transactionResult.TransactionId.Xor(disambiguationHash).ToStorageKey(),
                transactionResult);
        }
        
        public async Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults, Hash disambiguationHash)
        {
            await _transactionResultStore.SetAllAsync(
                transactionResults.ToDictionary(t => t.TransactionId.Xor(disambiguationHash).ToStorageKey(), t => t));
        }

        public async Task RemoveTransactionResultAsync(Hash txId, Hash disambiguationHash)
        {
            await _transactionResultStore.RemoveAsync(txId.Xor(disambiguationHash).ToStorageKey());
        }

        public async Task RemoveTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash)
        {
            await _transactionResultStore.RemoveAllAsync(txIds.Select(t => t.Xor(disambiguationHash).ToStorageKey())
                .ToList());
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash)
        {
            return await _transactionResultStore.GetAsync(txId.Xor(disambiguationHash).ToStorageKey());
        }
        
        public async Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash)
        {
            return await _transactionResultStore.GetAllAsync(txIds.Select(t => t.Xor(disambiguationHash).ToStorageKey())
                .ToList());
        }

        public async Task<bool> HasTransactionResultsAsync(Hash transactionId, Hash disambiguationHash)
        {
            return await _transactionResultStore.IsExistsAsync(transactionId.Xor(disambiguationHash).ToStorageKey());
        }
    }
}