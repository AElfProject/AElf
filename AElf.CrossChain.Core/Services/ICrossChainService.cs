using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash,
            long preBlockHeight);
        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash,
            long preBlockHeight);
        Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockInfo,
            Hash previousBlockHash, long preBlockHeight);
        Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockInfo,
            Hash previousBlockHash, long preBlockHeight);

        void CreateNewSideChainBlockInfoCache();
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight);
    }
}