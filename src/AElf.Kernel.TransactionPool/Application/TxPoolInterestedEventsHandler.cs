using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        public ILogger<TxPoolInterestedEventsHandler> Logger { get; set; }


        public TxPoolInterestedEventsHandler(ITxHub txHub)
        {
            _txHub = txHub;
            Logger = NullLogger<TxPoolInterestedEventsHandler>.Instance;
        }

        public async Task HandleEventAsync(TransactionsReceivedEvent eventData)
        {
            Logger.LogTrace($"## TransactionsReceivedEvent: {ChainHelpers.GetEventReceivedTimeSpan(eventData.CreateTime)} ms");
            await _txHub.HandleTransactionsReceivedAsync(eventData);
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            Logger.LogTrace($"## BlockAcceptedEvent: {ChainHelpers.GetEventReceivedTimeSpan(eventData.CreateTime)} ms");
            await _txHub.HandleBlockAcceptedAsync(eventData);
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
        {
            Logger.LogTrace($"## BestChainFoundEvent: {ChainHelpers.GetEventReceivedTimeSpan(eventData.CreateTime)} ms");
            await _txHub.HandleBestChainFoundAsync(eventData);
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            Logger.LogTrace($"## NewIrreversibleBlockFoundEvent: {ChainHelpers.GetEventReceivedTimeSpan(eventData.CreateTime)} ms");
            await _txHub.HandleNewIrreversibleBlockFoundAsync(eventData);
        }

        public async Task HandleEventAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            Logger.LogTrace($"## UnexecutableTransactionsFoundEvent: {ChainHelpers.GetEventReceivedTimeSpan(eventData.CreateTime)} ms");
            await _txHub.HandleUnexecutableTransactionsFoundAsync(eventData);
        }
    }
}