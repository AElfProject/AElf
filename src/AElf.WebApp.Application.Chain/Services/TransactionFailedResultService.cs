using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Services;


public interface ITransactionFailedResultService
{
    Task AddFailedTransactionResultsAsync(Hash transactionId, TransactionFailedResult transactionResult);
    Task<TransactionFailedResult> GetFailedTransactionResultAsync(Hash transactionId);
}

public class TransactionFailedResultService : ITransactionFailedResultService, ITransientDependency
{
    private readonly ITransactionFailedResultManager _transactionFailedResultManager;

    public TransactionFailedResultService(ITransactionFailedResultManager transactionFailedResultManager)
    {
        _transactionFailedResultManager = transactionFailedResultManager;
    }


    public async Task AddFailedTransactionResultsAsync(Hash transactionId, TransactionFailedResult transactionResult)
    {
        await _transactionFailedResultManager.AddFailedTransactionResultAsync(transactionId, transactionResult);
    }

    public async Task<TransactionFailedResult> GetFailedTransactionResultAsync(Hash transactionId)
    {
        return await _transactionFailedResultManager.GetFailedTransactionResultAsync(transactionId);
    }
}