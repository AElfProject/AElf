using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;


public interface ITransactionInvalidResultService
{
    Task AddFailedTransactionResultsAsync(InvalidTransactionResult transactionResult);
    Task<InvalidTransactionResult> GetTransactionInvalidResultAsync(Hash transactionId);
}

public class TransactionInvalidResultService : ITransactionInvalidResultService, ITransientDependency
{
    private readonly ITransactionInvalidResultManager _transactionInvalidResultManager;

    public TransactionInvalidResultService(ITransactionInvalidResultManager transactionInvalidResultManager)
    {
        _transactionInvalidResultManager = transactionInvalidResultManager;
    }
    
    public async Task AddFailedTransactionResultsAsync(InvalidTransactionResult transactionResult)
    {
        await _transactionInvalidResultManager.AddFailedTransactionResultAsync(transactionResult);
    }

    public async Task<InvalidTransactionResult> GetTransactionInvalidResultAsync(Hash transactionId)
    {
        return await _transactionInvalidResultManager.GetFailedTransactionResultAsync(transactionId);
    }
}