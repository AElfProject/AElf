using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionMethodValidationProvider : ITransactionValidationProvider
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        public ILocalEventBus LocalEventBus { get; set; }

        public TransactionMethodValidationProvider(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public bool ValidateWhileSyncing { get; } = false;
        public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext = null)
        {
            var isView = await _transactionReadOnlyExecutionService.IsViewTransactionAsync(chainContext, transaction);
            if (isView)
            {
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transaction.GetHash(),
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "View transaction is not allowed."
                });
            }

            return !isView;
        }
    }
}