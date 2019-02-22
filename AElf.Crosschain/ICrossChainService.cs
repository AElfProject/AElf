using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(int chainId);
        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(int chainId);
        Task<bool> ValidateSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockInfo);
        Task<bool> ValidateParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockInfo);

    }
}