using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ITransactionFailedResultManager
{
    Task AddFailedTransactionResultAsync(TransactionValidationFailure transactionResult);
    Task<TransactionValidationFailure> GetFailedTransactionResultAsync(Hash transactionId);
}

public class TransactionFailedResultManager : ITransactionFailedResultManager
{
    private readonly IBlockchainStore<TransactionValidationFailure> _transactionFailedResultStore;

    public TransactionFailedResultManager(IBlockchainStore<TransactionValidationFailure> transactionFailedResultStore)
    {
        _transactionFailedResultStore = transactionFailedResultStore;
    }

    public async Task AddFailedTransactionResultAsync(TransactionValidationFailure transactionResult)
    {
        await _transactionFailedResultStore.SetAsync(transactionResult.TransactionId.ToStorageKey(), transactionResult);
    }


    public async Task<TransactionValidationFailure> GetFailedTransactionResultAsync(Hash transactionId)
    {
        return await _transactionFailedResultStore.GetAsync(transactionId.ToStorageKey());
    }

}