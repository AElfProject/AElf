using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.TransactionPool.Infrastructure;

public class TransactionMethodValidationProvider : ITransactionValidationProvider
{
    private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

    public TransactionMethodValidationProvider(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
    {
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public bool ValidateWhileSyncing { get; } = false;

    public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext = null)
    {
        return true;
    }
}