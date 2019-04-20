using System.Collections.Generic;
using AElf.CrossChain.Cache.Exception;
using AElf.Kernel;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public class CrossChainMemCacheService : ICrossChainMemCacheService, ITransientDependency
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public CrossChainMemCacheService(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            _multiChainBlockInfoCacheProvider = multiChainBlockInfoCacheProvider;
        }

        public int GetCachedChainCount()
        {
            return _multiChainBlockInfoCacheProvider.Size;
        }

        public void TryRegisterNewChainCache(int remoteChainId, long chainHeight = KernelConstants.GenesisBlockHeight)
        {
            if(!_multiChainBlockInfoCacheProvider.ContainsChain(remoteChainId))
                _multiChainBlockInfoCacheProvider.AddBlockInfoCache(remoteChainId, new BlockInfoCache(chainHeight));
        }

        public long GetChainHeightNeeded(int chainId)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            if (blockInfoCache == null)
                throw new ChainCacheNotFoundException($"Chain {ChainHelpers.ConvertChainIdToBase58(chainId)} cache not found.");
            return blockInfoCache.TargetChainHeight();
        }

        public IEnumerable<int> GetCachedChainIds()
        {
            return _multiChainBlockInfoCacheProvider.CachedChainIds;
        }
    }
}