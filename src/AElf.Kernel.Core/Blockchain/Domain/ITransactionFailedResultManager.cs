using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ITransactionFailedResultManager
{
    Task AddFailedTransactionResultAsync(Hash transactionId, TransactionFailedResult transactionResult);
    Task<TransactionFailedResult> GetFailedTransactionResultAsync(Hash transactionId);
}

public class TransactionFailedResultManager : ITransactionFailedResultManager
{
    private readonly IBlockchainStore<TransactionFailedResult> _transactionFailedResultStore;

    public TransactionFailedResultManager(IBlockchainStore<TransactionFailedResult> transactionFailedResultStore)
    {
        _transactionFailedResultStore = transactionFailedResultStore;
    }

    public async Task AddFailedTransactionResultAsync(Hash transactionId, TransactionFailedResult transactionResult)
    {
        await _transactionFailedResultStore.SetAsync(transactionId.ToStorageKey(), transactionResult);
    }


    public async Task<TransactionFailedResult> GetFailedTransactionResultAsync(Hash transactionId)
    {
        var failedResult = await _transactionFailedResultStore.GetAsync(transactionId.ToStorageKey());
        if (failedResult != null)
        {
            failedResult.TransactionId = transactionId;
        }
        return failedResult;
    }

}