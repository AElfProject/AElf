using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ParallelRelatedEventsHandler : ILocalEventHandler<TransactionResourcesNeededEvent>,
        ILocalEventHandler<TransactionResourcesNoLongerNeededEvent>,
        ITransientDependency
    {
        private readonly IResourceExtractionService _resourceExtractionService;
        public ILogger<ParallelRelatedEventsHandler> Logger { get; set; }

        public ParallelRelatedEventsHandler(IResourceExtractionService resourceExtractionService)
        {
            _resourceExtractionService = resourceExtractionService;
            Logger = NullLogger<ParallelRelatedEventsHandler>.Instance;
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
