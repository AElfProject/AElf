using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public class TransactionManager : ITransactionManager
{
    private readonly IBlockchainStore<Transaction> _transactionStore;

    public TransactionManager(IBlockchainStore<Transaction> transactionStore)
    {
        _transactionStore = transactionStore;
        Logger = NullLogger<TransactionManager>.Instance;
    }

    public ILogger<TransactionManager> Logger { get; set; }

    public async Task<Hash> AddTransactionAsync(Transaction tx)
    {
        var transactionId = tx.GetHash();
        await _transactionStore.SetAsync(GetStringKey(transactionId), tx);
        return transactionId;
    }

    public async Task AddTransactionsAsync(IList<Transaction> txs)
    {
        await _transactionStore.SetAllAsync(txs.ToDictionary(t => GetStringKey(t.GetHash()), t => t));
    }

    public async Task<Transaction> GetTransactionAsync(Hash txId)
    {
        return await _transactionStore.GetAsync(GetStringKey(txId));
    }

    public async Task<List<Transaction>> GetTransactionsAsync(IList<Hash> txIds)
    {
        return await _transactionStore.GetAllAsync(txIds.Select(GetStringKey).ToList());
    }

    public async Task RemoveTransactionAsync(Hash txId)
    {
        await _transactionStore.RemoveAsync(GetStringKey(txId));
    }

    public async Task RemoveTransactionsAsync(IList<Hash> txIds)
    {
        await _transactionStore.RemoveAllAsync(txIds.Select(GetStringKey).ToList());
    }


    public async Task<bool> HasTransactionAsync(Hash txId)
    {
        return await _transactionStore.IsExistsAsync(GetStringKey(txId));
    }

    private string GetStringKey(Hash txId)
    {
        return txId.ToStorageKey();
    }
}