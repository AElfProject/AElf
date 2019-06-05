using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ParallelRelatedEventsHandler : ILocalEventHandler<TxResourcesNeededEvent>,
        ILocalEventHandler<TxResourcesNoLongerNeededEvent>,
        ITransientDependency
    {
        private readonly IResourceExtractionService _resourceExtractionService;

        public ParallelRelatedEventsHandler(IResourceExtractionService resourceExtractionService)
        {
            _resourceExtractionService = resourceExtractionService;
        }

        public async Task HandleEventAsync(TxResourcesNeededEvent eventData)
        {
            await _resourceExtractionService.HandleTxResourcesNeededAsync(eventData);
        }
        
        public async Task HandleEventAsync(TxResourcesNoLongerNeededEvent eventData)
        {
            await _resourceExtractionService.HandleTxResourcesNoLongerNeededAsync(eventData);
        }
    }
}
