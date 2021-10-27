using System.Threading.Tasks;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class TransactionExecutionValidationFailedEventHandler :
        ILocalEventHandler<TransactionExecutionValidationFailedEvent>,
        ITransientDependency
    {
        private readonly IPeerInvalidTransactionProcessingService _peerInvalidTransactionProcessingService;

        public ILogger<TransactionExecutionValidationFailedEventHandler> Logger { get; set; }

        public TransactionExecutionValidationFailedEventHandler(
            IPeerInvalidTransactionProcessingService peerInvalidTransactionProcessingService)
        {
            _peerInvalidTransactionProcessingService = peerInvalidTransactionProcessingService;

            Logger = NullLogger<TransactionExecutionValidationFailedEventHandler>.Instance;
        }

        public Task HandleEventAsync(TransactionExecutionValidationFailedEvent eventData)
        {
            Logger.LogDebug($"Received a transaction that failed to verify: {eventData.TransactionId}");
            _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(eventData.TransactionId);

            return Task.CompletedTask;
        }
    }
}