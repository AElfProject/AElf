using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockInfo(List<SideChainBlockInfo> sideChainBlockInfo);
        Task<bool> GetParentChainBlockInfo(List<ParentChainBlockInfo> parentChainBlockInfo);
        void AddNewSideChainCache(IClientBase clientBase);
    }
}