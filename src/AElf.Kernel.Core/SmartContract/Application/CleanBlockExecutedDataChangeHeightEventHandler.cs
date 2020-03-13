using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Events;

namespace AElf.Kernel.SmartContract.Application
{
    public class CleanBlockExecutedDataChangeHeightBaseEventHandler<T>
    {
        private readonly ICachedBlockchainExecutedDataService<T> _cachedBlockchainExecutedDataService;
        
        public CleanBlockExecutedDataChangeHeightBaseEventHandler(ICachedBlockchainExecutedDataService<T> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }

        public Task HandleEventAsync(CleanBlockExecutedDataChangeHeightEventData eventData)
        {
            _cachedBlockchainExecutedDataService.CleanChangeHeight(eventData.IrreversibleBlockHeight);
            return Task.CompletedTask;
        }
    }
}