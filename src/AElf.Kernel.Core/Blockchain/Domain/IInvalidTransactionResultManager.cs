using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface IInvalidTransactionResultManager
{
    Task AddInvalidTransactionResultAsync(InvalidTransactionResult transactionResult);
    Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId);
}

public class InvalidTransactionResultManager : IInvalidTransactionResultManager
{
    private readonly IBlockchainStore<InvalidTransactionResult> _invalidTransactionResultStore;

    public InvalidTransactionResultManager(IBlockchainStore<InvalidTransactionResult> invalidTransactionResultStore)
    {
        _invalidTransactionResultStore = invalidTransactionResultStore;
    }

    public async Task AddInvalidTransactionResultAsync(InvalidTransactionResult transactionResult)
    {
        await _invalidTransactionResultStore.SetAsync(transactionResult.TransactionId.ToStorageKey(), transactionResult);
    }


    public async Task<InvalidTransactionResult> GetInvalidTransactionResultAsync(Hash transactionId)
    {
        return await _invalidTransactionResultStore.GetAsync(transactionId.ToStorageKey());
    }

}