using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ITransactionInvalidResultManager
{
    Task AddTransactionInvalidResultAsync(InvalidTransactionResult transactionResult);
    Task<InvalidTransactionResult> GetTransactionInvalidResultAsync(Hash transactionId);
}

public class TransactionInvalidResultManager : ITransactionInvalidResultManager
{
    private readonly IBlockchainStore<InvalidTransactionResult> _transactionInvalidResultStore;

    public TransactionInvalidResultManager(IBlockchainStore<InvalidTransactionResult> transactionInvalidResultStore)
    {
        _transactionInvalidResultStore = transactionInvalidResultStore;
    }

    public async Task AddTransactionInvalidResultAsync(InvalidTransactionResult transactionResult)
    {
        await _transactionInvalidResultStore.SetAsync(transactionResult.TransactionId.ToStorageKey(), transactionResult);
    }


    public async Task<InvalidTransactionResult> GetTransactionInvalidResultAsync(Hash transactionId)
    {
        return await _transactionInvalidResultStore.GetAsync(transactionId.ToStorageKey());
    }

}