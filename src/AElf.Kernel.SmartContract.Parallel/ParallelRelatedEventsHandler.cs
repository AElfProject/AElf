using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ParallelRelatedEventsHandler : ILocalEventHandler<TransactionResourcesNeededEvent>,
        ILocalEventHandler<TransactionResourcesNoLongerNeededEvent>,
        ITransientDependency
    {
        private readonly IResourceExtractionService _resourceExtractionService;

        public ParallelRelatedEventsHandler(IResourceExtractionService resourceExtractionService)
        {
            _resourceExtractionService = resourceExtractionService;
        }

        public async Task HandleEventAsync(TransactionResourcesNeededEvent eventData)
        {
            await _resourceExtractionService.HandleTransactionResourcesNeededAsync(eventData);
        }
        
        public async Task HandleEventAsync(TransactionResourcesNoLongerNeededEvent eventData)
        {
            await _resourceExtractionService.HandleTransactionResourcesNoLongerNeededAsync(eventData);
        }
    }
}
