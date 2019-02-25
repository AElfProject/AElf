using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<bool> ValidateSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockInfo,
            Hash previousBlockHash, ulong preBlockHeight);
        Task<bool> ValidateParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockInfo,
            Hash previousBlockHash, ulong preBlockHeight);

        void CreateNewSideChainBlockInfoCache(int chainId);
    }
}