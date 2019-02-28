using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TxPoolInterestedEventsHandler : ILocalEventHandler<BestChainFoundEvent>,
        ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ITransientDependency
    {
        private readonly IChainRelatedComponentManager<ITxHub> _txHubs;

        public TxPoolInterestedEventsHandler(IChainRelatedComponentManager<ITxHub> txHubs)
        {
            _txHubs = txHubs;
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
        {
            await _txHubs.Get(eventData.ChainId).HandleBestChainFoundAsync(eventData);
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _txHubs.Get(eventData.ChainId).HandleNewIrreversibleBlockFoundAsync(eventData);
        }
    }
}