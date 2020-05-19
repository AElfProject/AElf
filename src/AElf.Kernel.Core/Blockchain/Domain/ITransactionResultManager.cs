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
        Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash);
        Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash);
        Task<bool> HasTransactionResultAsync(Hash transactionId, Hash disambiguationHash);
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
            await _transactionResultStore.SetAsync(
                HashHelper.XorAndCompute(transactionResult.TransactionId, disambiguationHash).ToStorageKey(),
                transactionResult);
        }

        public async Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults,
            Hash disambiguationHash)
        {
            await _transactionResultStore.SetAllAsync(
                transactionResults.ToDictionary(t => HashHelper.XorAndCompute(t.TransactionId, disambiguationHash).ToStorageKey(), t => t));
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash)
        {
            return await _transactionResultStore.GetAsync(HashHelper.XorAndCompute(txId, disambiguationHash).ToStorageKey());
        }

        public async Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds,
            Hash disambiguationHash)
        {
            return await _transactionResultStore.GetAllAsync(txIds.Select(t => HashHelper.XorAndCompute(t, disambiguationHash).ToStorageKey())
                .ToList());
        }

        public async Task<bool> HasTransactionResultAsync(Hash transactionId, Hash disambiguationHash)
        {
            return await _transactionResultStore.IsExistsAsync(HashHelper.XorAndCompute(transactionId, disambiguationHash).ToStorageKey());
        }
    }
}