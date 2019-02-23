using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IMultiChainBlockInfoCache _multiChainBlockInfoCache;
        
        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, 
            IMultiChainBlockInfoCache multiChainBlockInfoCache)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _multiChainBlockInfoCache = multiChainBlockInfoCache;
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

        public void CreateNewSideChain(int chainId)
        {
            _multiChainBlockInfoCache.AddBlockInfoCache(chainId, new BlockInfoCache(0));
        }
    }
}