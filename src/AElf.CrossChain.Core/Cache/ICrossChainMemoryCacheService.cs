using System.Collections.Generic;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainMemoryCacheService
    {
        void RegisterNewChainCache(int remoteChainId, long chainHeight = Constants.GenesisBlockHeight);
        int GetCachedChainCount();
        long GetNeededChainHeightForCache(int chainId);
        IEnumerable<int> GetCachedChainIds();
    }
}