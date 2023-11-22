using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ITransactionResultManager
{
    Task AddTransactionResultAsync(TransactionResult transactionResult, Hash disambiguationHash);
    Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults, Hash disambiguationHash);
    Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash);
    Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds, Hash disambiguationHash);
    Task<bool> HasTransactionResultAsync(Hash transactionId, Hash disambiguationHash);
    Task AddFailedTransactionResultAsync(TransactionResult transactionResult);
    Task AddFailedTransactionResultsAsync(IList<TransactionResult> transactionResults);
    Task<TransactionResult> GetFailedTransactionResultAsync(Hash transactionId);
    Task<List<TransactionResult>> GetFailedTransactionResultsAsync(IList<Hash> txIds);
}

public class TransactionResultManager : ITransactionResultManager
{
    private const string FailStorageKeyPrefix = "FAIL:";
    private readonly IBlockchainStore<TransactionResult> _transactionResultStore;

    public TransactionResultManager(IBlockchainStore<TransactionResult> transactionResultStore)
    {
        _transactionResultStore = transactionResultStore;
    }

    public async Task AddFailedTransactionResultAsync(TransactionResult transactionResult)
    {
        await _transactionResultStore.SetAsync(FailStorageKeyPrefix + transactionResult.TransactionId.ToStorageKey(), transactionResult);
    }

    public async Task AddFailedTransactionResultsAsync(IList<TransactionResult> transactionResults)
    {
        await _transactionResultStore.SetAllAsync(
            transactionResults.ToDictionary(tx => FailStorageKeyPrefix + tx.TransactionId.ToStorageKey(), t => t)
        );
    }

    public async Task<TransactionResult> GetFailedTransactionResultAsync(Hash transactionId)
    {
        return await _transactionResultStore.GetAsync(FailStorageKeyPrefix + transactionId.ToStorageKey());
    }

    public async Task<List<TransactionResult>> GetFailedTransactionResultsAsync(IList<Hash> txIds)
    {
        return await _transactionResultStore.GetAllAsync(txIds.Select(id => FailStorageKeyPrefix + id.ToStorageKey()).ToList());
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
            transactionResults.ToDictionary(
                t => HashHelper.XorAndCompute(t.TransactionId, disambiguationHash).ToStorageKey(), t => t));
    }

    public async Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash)
    {
        return await _transactionResultStore.GetAsync(HashHelper.XorAndCompute(txId, disambiguationHash)
            .ToStorageKey());
    }

    public async Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds,
        Hash disambiguationHash)
    {
        return await _transactionResultStore.GetAllAsync(txIds
            .Select(t => HashHelper.XorAndCompute(t, disambiguationHash).ToStorageKey())
            .ToList());
    }

    public async Task<bool> HasTransactionResultAsync(Hash transactionId, Hash disambiguationHash)
    {
        return await _transactionResultStore.IsExistsAsync(HashHelper.XorAndCompute(transactionId, disambiguationHash)
            .ToStorageKey());
    }
}