using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainService : ICrossChainService, ITransientDependency
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IChainManager _chainManager;
        private ILocalEventBus LocalEventBus { get; }

        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, IChainManager chainManager)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _chainManager = chainManager;
            LocalEventBus = NullLocalEventBus.Instance;
            LocalEventBus.Subscribe<BestChainFoundEventData>(RegisterSideChainAsync);
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash,
            long preBlockHeight)
        {
            return await _crossChainDataProvider.GetSideChainBlockDataAsync(previousBlockHash, preBlockHeight);
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash,
            long preBlockHeight)
        {
            return await _crossChainDataProvider.GetParentChainBlockDataAsync(previousBlockHash, preBlockHeight);
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(
            List<SideChainBlockData> sideChainBlockData, Hash previousBlockHash, long preBlockHeight)
        {
            return await _crossChainDataProvider.ValidateSideChainBlockDataAsync(sideChainBlockData, 
                previousBlockHash, preBlockHeight);
        }
        
        public async Task<bool> ValidateParentChainBlockDataAsync(
            List<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, long preBlockHeight)
        {
            return await _crossChainDataProvider.ValidateParentChainBlockDataAsync(parentChainBlockData, 
                previousBlockHash, preBlockHeight);
        }

        public void CreateNewSideChainBlockInfoCache()
        {
            _crossChainDataProvider.RegisterNewChain(_chainManager.GetChainId());
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            return await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(previousBlockHash,
                previousBlockHeight);
        }

        private async Task RegisterSideChainAsync(BestChainFoundEventData eventData)
        {
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(eventData.BlockHash,
                eventData.BlockHeight);
        }
    }
}