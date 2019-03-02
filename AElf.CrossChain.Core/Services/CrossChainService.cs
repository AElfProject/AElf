using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private ILocalEventBus LocalEventBus { get; }

        public CrossChainService(ICrossChainDataProvider crossChainDataProvider)
        {
            _crossChainDataProvider = crossChainDataProvider;
            LocalEventBus = NullLocalEventBus.Instance;
            LocalEventBus.Subscribe<BestChainFoundEventData>(RegisterSideChainAsync);
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var res = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockDataAsync(res, previousBlockHash, preBlockHeight);
            return res;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var res = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockDataAsync(res, previousBlockHash, preBlockHeight);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(
            IList<SideChainBlockData> sideChainBlockData, Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainDataProvider.GetSideChainBlockDataAsync(sideChainBlockData, 
                previousBlockHash, preBlockHeight, true);
        }
        
        public async Task<bool> ValidateParentChainBlockDataAsync(
            IList<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainDataProvider.GetParentChainBlockDataAsync(parentChainBlockData, 
                previousBlockHash, preBlockHeight, true);
        }

        public void CreateNewSideChainBlockInfoCache()
        {
            _crossChainDataProvider.RegisterNewChain();
        }

        private async Task RegisterSideChainAsync(BestChainFoundEventData eventData)
        {
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(eventData.BlockHash,
                eventData.BlockHeight);
        }
    }
}