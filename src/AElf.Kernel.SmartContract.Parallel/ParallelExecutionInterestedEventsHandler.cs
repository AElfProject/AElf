using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ParallelExecutionInterestedEventsHandler : ILocalEventHandler<TransactionAcceptedEvent>,
        ILocalEventHandler<BlockAcceptedEvent>,
        ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ILocalEventHandler<UnexecutableTransactionsFoundEvent>,
        ITransientDependency
    {
        private readonly IResourceExtractionService _resourceExtractionService;

        public ParallelExecutionInterestedEventsHandler(IResourceExtractionService resourceExtractionService)
        {
            _resourceExtractionService = resourceExtractionService;
        }

        public async Task HandleEventAsync(TransactionAcceptedEvent eventData)
        {
            await _resourceExtractionService.HandleTransactionAcceptedEvent(eventData);
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _resourceExtractionService.HandleBlockAcceptedAsync(eventData);
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _resourceExtractionService.HandleNewIrreversibleBlockFoundAsync(eventData);
        }

        public async Task HandleEventAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            await _resourceExtractionService.HandleUnexecutableTransactionsFoundAsync(eventData);
        }
    }
}
