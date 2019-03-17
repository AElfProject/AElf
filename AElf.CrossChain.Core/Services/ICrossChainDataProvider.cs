using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainDataProvider
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash, long preBlockHeight);

        Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, long preBlockHeight);

        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash, long preBlockHeight);

        Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockData,
            Hash previousBlockHash, long preBlockHeight);

        Task ActivateCrossChainCacheAsync(Hash blockHash, long blockHeight);

        void RegisterNewChain(int chainId);
        //void AddNewSideChainDataConsumer(ICrossChainDataConsumer crossChainDataConsumer);
        //int GetCachedChainCount();
        //void CreateNewSideChain();
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight);

        Task<CrossChainBlockData> GetNewCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight);

        CrossChainBlockData GetUsedCrossChainBlockData(Hash previousBlockHash, long previousBlockHeight);
    }
}