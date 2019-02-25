using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainDataProvider
    {
        Task<bool> GetSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false);
        Task<bool> GetParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false);

        Task CheckCrossChainCacheAsync(int chainId, ulong blockHeight, Hash blockHash);

        void RegisterNewChain(int chainId);
        //void AddNewSideChainDataConsumer(ICrossChainDataConsumer crossChainDataConsumer);
        //int GetCachedChainCount();
        //void CreateNewSideChain(int chainId);
    }
}