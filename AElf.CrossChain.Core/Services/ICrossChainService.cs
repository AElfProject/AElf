using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash,
            ulong preBlockHeight);
        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash,
            ulong preBlockHeight);
        Task<bool> ValidateSideChainBlockDataAsync(IList<SideChainBlockData> sideChainBlockInfo,
            Hash previousBlockHash, ulong preBlockHeight);
        Task<bool> ValidateParentChainBlockDataAsync(IList<ParentChainBlockData> parentChainBlockInfo,
            Hash previousBlockHash, ulong preBlockHeight);

        void CreateNewSideChainBlockInfoCache();
    }
}