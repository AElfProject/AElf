using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainService : ITransientDependency
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IChainManager _chainManager;

        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, IChainManager chainManager)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _chainManager = chainManager;
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

        public async Task<CrossChainBlockData> GetNewCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            return await _crossChainDataProvider.GetCrossChainBlockDataForNextMiningAsync(previousBlockHash, previousBlockHeight);
        }

        public CrossChainBlockData GetCrossChainBlockDataFilledInBlock(Hash previousBlockHash, long previousBlockHeight)
        {
            return _crossChainDataProvider.GetUsedCrossChainBlockDataForLastMiningAsync(previousBlockHash, previousBlockHeight);
        }

        public async Task<CrossChainBlockData> GetCrossChainBlockDataIndexedInStateAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            return await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(previousBlockHash,
                previousBlockHeight);
        }

        public async Task<ChainInitializationContext> GetChainInitializationContextAsync(int chainId)
        {
            return await _crossChainDataProvider.GetChainInitializationContextAsync(chainId);
        }
    }
}