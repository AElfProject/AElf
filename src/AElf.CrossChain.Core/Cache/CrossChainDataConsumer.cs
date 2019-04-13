using AElf.Kernel;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumer : ICrossChainDataConsumer, ISingletonDependency
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public CrossChainDataConsumer(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            _multiChainBlockInfoCacheProvider = multiChainBlockInfoCacheProvider;
        }

        public T TryTake<T>(int crossChainId, long height, bool isCacheSizeLimited)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(crossChainId);
            if (blockInfoCache == null)
                return default(T);
            if (blockInfoCache.TryTake(height, out var blockInfo, isCacheSizeLimited))
            {
                return (T) blockInfo;
            }

            return default(T);
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

        public bool CheckAlreadyCachedChain(int remoteChainId)
        {
            return _multiChainBlockInfoCacheProvider.ContainsChain(remoteChainId);
        }
    }
}