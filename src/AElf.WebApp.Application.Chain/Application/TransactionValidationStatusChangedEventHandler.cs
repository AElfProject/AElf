using System.Threading.Tasks;
using AElf.Kernel.TransactionPool;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.EventBus;

namespace AElf.WebApp.Application.Chain.Application
{
    public class TransactionValidationStatusChangedEventHandler : ILocalEventHandler<TransactionValidationStatusChangedEvent>
    {
        private readonly ITransactionResultStatusCacheProvider _transactionResultStatusCacheProvider;

        public TransactionValidationStatusChangedEventHandler(
            ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider)
        {
            _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
        }

        public Task HandleEventAsync(TransactionValidationStatusChangedEvent eventData)
        {
            _transactionResultStatusCacheProvider.SetTransactionResultStatus(eventData.TransactionId,
                new TransactionValidateStatus
                {
                    TransactionResultStatus = eventData.TransactionResultStatus,
                    Error = eventData.Error
                });
            return Task.CompletedTask;
        }
    }
}