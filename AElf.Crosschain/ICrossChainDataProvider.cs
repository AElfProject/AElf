using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockData(IList<SideChainBlockData> sideChainBlockData);
        Task<bool> GetParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData);
        void AddNewSideChainCache(IClientBase clientBase);
    }
}