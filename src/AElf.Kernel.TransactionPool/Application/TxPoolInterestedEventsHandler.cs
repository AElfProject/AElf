using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TxPoolInterestedEventsHandler : ILocalEventHandler<TransactionsReceivedEvent>,
        ILocalEventHandler<ExecutableTransactionsReceivedEvent>,
        ILocalEventHandler<BlockAcceptedEvent>,
        ILocalEventHandler<BestChainFoundEventData>,
        ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ILocalEventHandler<UnexecutableTransactionsFoundEvent>,
        ITransientDependency
    {
        private readonly ITxHub _txHub;
        private readonly IResourceExtractionService _resourceExtractionService;

        public TxPoolInterestedEventsHandler(ITxHub txHub, IResourceExtractionService resourceExtractionService)
        {
            _txHub = txHub;
            _resourceExtractionService = resourceExtractionService;
        }


        public async Task HandleEventAsync(TransactionsReceivedEvent eventData)
        {
            await _txHub.HandleTransactionsReceivedAsync(eventData);
        }
        
        public async Task HandleEventAsync(ExecutableTransactionsReceivedEvent eventData)
        {
            await _resourceExtractionService.HandleExecutableTransactionsReceivedAsync(eventData);
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _txHub.HandleBlockAcceptedAsync(eventData);
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            await _txHub.HandleBestChainFoundAsync(eventData);
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _txHub.HandleNewIrreversibleBlockFoundAsync(eventData);
        }

        public async Task HandleEventAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            await _txHub.HandleUnexecutableTransactionsFoundAsync(eventData);
        }
    }
}