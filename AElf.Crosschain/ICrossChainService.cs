using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        Task<List<SideChainBlockInfo>> GetSideChainBlockInfo();
        Task<List<ParentChainBlockInfo>> GetParentChainBlockInfo();
        Task<bool> ValidateSideChainBlockInfo(List<SideChainBlockInfo> sideChainBlockInfo);
        Task<bool> ValidateParentChainBlockInfo(List<ParentChainBlockInfo> parentChainBlockInfo);

        void IndexNewSideChain(IClientBase clientBase);
    }
}