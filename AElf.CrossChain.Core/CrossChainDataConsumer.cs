namespace AElf.CrossChain
{
    public class CrossChainDataConsumer : ICrossChainDataConsumer
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public CrossChainDataConsumer(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            _multiChainBlockInfoCacheProvider = multiChainBlockInfoCacheProvider;
        }

        public IBlockInfo TryTake(int crossChainId, ulong height, bool isCacheSizeLimited)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(crossChainId);
            if (blockInfoCache == null)
                return null;
            return blockInfoCache.TryTake(height, out var blockInfo, isCacheSizeLimited) ? blockInfo : null;
        }

        public int GetCachedChainCount()
        {
            return _multiChainBlockInfoCacheProvider.Size;
        }

        public void RegisterNewChainCache(int chainId, ulong chainHeight)
        {
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, new BlockInfoCache(chainHeight));
        }
    }
}