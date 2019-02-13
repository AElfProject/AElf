using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockData>> GetSideChainBlockData();
        Task<List<ParentChainBlockData>> GetParentChainBlockData();
        Task<bool> ValidateSideChainBlockData(IList<SideChainBlockData> sideChainBlockInfo);
        Task<bool> ValidateParentChainBlockData(IList<ParentChainBlockData> parentChainBlockInfo);

    }
}