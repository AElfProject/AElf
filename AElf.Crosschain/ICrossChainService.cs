using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockInfo();
        Task<List<ParentChainBlockData>> GetParentChainBlockInfo();
        Task<bool> ValidateSideChainBlockInfo(List<SideChainBlockData> sideChainBlockInfo);
        Task<bool> ValidateParentChainBlockInfo(List<ParentChainBlockData> parentChainBlockInfo);

    }
}