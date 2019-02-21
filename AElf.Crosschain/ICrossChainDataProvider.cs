using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockData(IList<SideChainBlockData> sideChainBlockData, bool isValidation = false);
        Task<bool> GetParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData, bool isValidation = false);
        void AddNewSideChainCache(IClientBase clientBase);
        int GetCachedChainCount();
    }
}