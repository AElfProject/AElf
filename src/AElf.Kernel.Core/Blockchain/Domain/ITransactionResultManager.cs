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
            transactionResult.StorageKey,
            transactionResult);
    }

    public async Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults,
        Hash disambiguationHash)
    {
        await _transactionResultStore.SetAllAsync(
            transactionResults.ToDictionary(t => t.StorageKey, t => t));
    }

    public async Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash)
    {
        return await _transactionResultStore.GetAsync(GetStorageKey(txId));
    }

    public async Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> txIds,
        Hash disambiguationHash)
    {
        return await _transactionResultStore.GetAllAsync(txIds.Select(t => GetStorageKey(t))
            .ToList());
    }

    public async Task<bool> HasTransactionResultAsync(Hash transactionId, Hash disambiguationHash)
    {
        return await _transactionResultStore.IsExistsAsync(GetStorageKey(transactionId));
    }

    private string GetStorageKey(Hash txId)
    {
        return txId.ToStorageKey();
    }
}