using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockDataAsync(IList<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false);
        Task<bool> GetParentChainBlockDataAsync(IList<ParentChainBlockData> parentChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false);

        Task<bool> ActivateCrossChainCacheAsync(Hash blockHash, ulong blockHeight);

        void RegisterNewChain();
        //void AddNewSideChainDataConsumer(ICrossChainDataConsumer crossChainDataConsumer);
        //int GetCachedChainCount();
        //void CreateNewSideChain();
    }
}