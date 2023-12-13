using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ITransactionInvalidResultManager
{
    Task AddFailedTransactionResultAsync(InvalidTransactionResult transactionResult);
    Task<InvalidTransactionResult> GetFailedTransactionResultAsync(Hash transactionId);
}

public class TransactionInvalidResultManager : ITransactionInvalidResultManager
{
    private readonly IBlockchainStore<InvalidTransactionResult> _transactionFailedResultStore;

    public TransactionInvalidResultManager(IBlockchainStore<InvalidTransactionResult> transactionFailedResultStore)
    {
        _transactionFailedResultStore = transactionFailedResultStore;
    }

    public async Task AddFailedTransactionResultAsync(InvalidTransactionResult transactionResult)
    {
        await _transactionFailedResultStore.SetAsync(transactionResult.TransactionId.ToStorageKey(), transactionResult);
    }


    public async Task<InvalidTransactionResult> GetFailedTransactionResultAsync(Hash transactionId)
    {
        return await _transactionFailedResultStore.GetAsync(transactionId.ToStorageKey());
    }

}