using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockInfo(List<SideChainBlockData> sideChainBlockInfo);
        Task<bool> GetParentChainBlockInfo(List<ParentChainBlockData> parentChainBlockInfo);
        void AddNewSideChainCache(IClientBase clientBase);
    }
}