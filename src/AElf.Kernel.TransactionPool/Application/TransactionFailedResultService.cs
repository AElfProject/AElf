using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;


public interface ITransactionFailedResultService
{
    Task AddFailedTransactionResultsAsync(TransactionValidationFailure transactionResult);
    Task<TransactionValidationFailure> GetFailedTransactionResultAsync(Hash transactionId);
}

public class TransactionFailedResultService : ITransactionFailedResultService, ITransientDependency
{
    private readonly ITransactionFailedResultManager _transactionFailedResultManager;

    public TransactionFailedResultService(ITransactionFailedResultManager transactionFailedResultManager)
    {
        _transactionFailedResultManager = transactionFailedResultManager;
    }
    
    public async Task AddFailedTransactionResultsAsync(TransactionValidationFailure transactionResult)
    {
        await _transactionFailedResultManager.AddFailedTransactionResultAsync(transactionResult);
    }

    public async Task<TransactionValidationFailure> GetFailedTransactionResultAsync(Hash transactionId)
    {
        return await _transactionFailedResultManager.GetFailedTransactionResultAsync(transactionId);
    }
}