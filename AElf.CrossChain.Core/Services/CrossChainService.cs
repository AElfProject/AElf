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
            LocalEventBus.Subscribe<BestChainFoundEvent>(RegisterSideChainAsync);
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var res = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockDataAsync(chainId, res, previousBlockHash, preBlockHeight);
            return res;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var res = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockDataAsync(chainId, res, previousBlockHash, preBlockHeight);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(int chainId,
            IList<SideChainBlockData> sideChainBlockData, Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainDataProvider.GetSideChainBlockDataAsync(chainId, sideChainBlockData, 
                previousBlockHash, preBlockHeight, true);
        }
        
        public async Task<bool> ValidateParentChainBlockDataAsync(int chainId,
            IList<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainDataProvider.GetParentChainBlockDataAsync(chainId, parentChainBlockData, 
                previousBlockHash, preBlockHeight, true);
        }

        public void CreateNewSideChainBlockInfoCache(int chainId)
        {
            _crossChainDataProvider.RegisterNewChain(chainId);
        }

        private async Task RegisterSideChainAsync(BestChainFoundEvent eventData)
        {
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(eventData.ChainId, eventData.BlockHash,
                eventData.BlockHeight);
        }
    }
}