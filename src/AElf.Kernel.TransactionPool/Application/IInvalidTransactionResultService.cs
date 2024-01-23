using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;


public interface IInvalidTransactionResultService
{
    Task AddInvalidTransactionResultAsync(InvalidTransactionResult invalidTransactionResult);
    Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId);
}

public class InvalidTransactionResultService : IInvalidTransactionResultService, ITransientDependency
{
    private readonly IInvalidTransactionResultManager _invalidTransactionResultManager;

    public InvalidTransactionResultService(IInvalidTransactionResultManager invalidTransactionResultManager)
    {
        _invalidTransactionResultManager = invalidTransactionResultManager;
    }
    
    public async Task AddInvalidTransactionResultAsync(InvalidTransactionResult invalidTransactionResult)
    {
        await _invalidTransactionResultManager.AddInvalidTransactionResultAsync(invalidTransactionResult);
    }

    public async Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId)
    {
        return await _invalidTransactionResultManager.GetInvalidTransactionResultAsync(transactionId);
    }
}