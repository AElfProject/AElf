using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockData(int chainId);
        Task<List<ParentChainBlockData>> GetParentChainBlockData(int chainId);
        Task<bool> ValidateSideChainBlockData(int chainId, IList<SideChainBlockData> sideChainBlockInfo);
        Task<bool> ValidateParentChainBlockData(int chainId, IList<ParentChainBlockData> parentChainBlockInfo);

    }
}