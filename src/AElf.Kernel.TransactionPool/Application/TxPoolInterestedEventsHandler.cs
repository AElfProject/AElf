using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TxPoolInterestedEventsHandler : ILocalEventHandler<TransactionsReceivedEvent>,
        ILocalEventHandler<BlockAcceptedEvent>,
        ILocalEventHandler<BestChainFoundEvent>,
        ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ILocalEventHandler<UnexecutableTransactionsFoundEvent>,
        ITransientDependency
    {
        private readonly ITxHub _txHub;

        public TxPoolInterestedEventsHandler(ITxHub txHub)
        {
            _txHub = txHub;
        }

        public async Task HandleEventAsync(TransactionsReceivedEvent eventData)
        {
            await _txHub.HandleTransactionsReceivedAsync(eventData);
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _txHub.HandleBlockAcceptedAsync(eventData);
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
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