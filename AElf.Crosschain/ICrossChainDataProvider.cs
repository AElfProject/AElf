using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockData, bool isValidation = false);
        Task<bool> GetParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockData, bool isValidation = false);
        //void AddNewSideChainDataConsumer(ICrossChainDataConsumer crossChainDataConsumer);
        int GetCachedChainCount();
    }
}