using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainMemCacheService
    {
        void TryRegisterNewChainCache(int remoteChainId, long chainHeight = KernelConstants.GenesisBlockHeight);
        int GetCachedChainCount();
        long GetChainHeightNeeded(int chainId);
        IEnumerable<int> GetCachedChainIds();
    }
}