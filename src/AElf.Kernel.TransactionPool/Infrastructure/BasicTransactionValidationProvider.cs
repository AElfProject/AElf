using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class BasicTransactionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => true;
        public ILocalEventBus LocalEventBus { get; set; }

        public BasicTransactionValidationProvider()
        {
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            var transactionId = transaction.GetHash();
            if (!transaction.VerifySignature())
            {
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "Incorrect transaction signature."
                });
                return false;
            }

            if (transaction.CalculateSize() > TransactionPoolConsts.TransactionSizeLimit)
            {
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "Transaction size exceeded."
                });
                return false;
            }

            return true;
        }
    }
}