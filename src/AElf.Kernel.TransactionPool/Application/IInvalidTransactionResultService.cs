using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;


public interface IInvalidTransactionResultService
{
    Task AddInvalidTransactionResultsAsync(InvalidTransactionResult transactionResult);
    Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId);
}

public class InvalidTransactionResultService : IInvalidTransactionResultService, ITransientDependency
{
    private readonly IInvalidTransactionResultManager _invalidTransactionResultManager;

    public InvalidTransactionResultService(IInvalidTransactionResultManager invalidTransactionResultManager)
    {
        _invalidTransactionResultManager = invalidTransactionResultManager;
    }
    
    public async Task AddInvalidTransactionResultsAsync(InvalidTransactionResult transactionResult)
    {
        await _invalidTransactionResultManager.AddInvalidTransactionResultAsync(transactionResult);
    }

    public async Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId)
    {
        return await _invalidTransactionResultManager.GetInvalidTransactionResultAsync(transactionId);
    }
}