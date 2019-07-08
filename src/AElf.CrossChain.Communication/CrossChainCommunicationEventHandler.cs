using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Events;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandler : ILocalEventHandler<CrossChainDataValidatedEvent>, ILocalEventHandler<InitialSyncFinishedEvent>, ILocalEventHandler<NewIrreversibleBlockFoundEvent>
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;

        public CrossChainCommunicationEventHandler(ICrossChainRequestService crossChainRequestService, 
            ICrossChainCacheEntityService crossChainCacheEntityService, IIrreversibleBlockStateProvider irreversibleBlockStateProvider)
        {
            _crossChainRequestService = crossChainRequestService;
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
        }

        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            var isReadyToRequest = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistsAsync();
            if (!isReadyToRequest)
                return;
            _ = _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }

        public async Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            var isReadyToCreateChainCache = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistsAsync();
            if (!isReadyToCreateChainCache)
                return;
            var libIdHeight = await _irreversibleBlockStateProvider.GetLibHashAndHeightAsync();
            _ = _crossChainCacheEntityService.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
        }

        public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            return _crossChainCacheEntityService.RegisterNewChainsAsync(eventData.BlockHash, eventData.BlockHeight);
        }
    }
}