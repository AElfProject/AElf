namespace AElf.CrossChain
{
    public class CrossChainDataConsumer : ICrossChainDataConsumer
    {
        private readonly IMultiChainBlockInfoCache _multiChainBlockInfoCache;

        public CrossChainDataConsumer(IMultiChainBlockInfoCache multiChainBlockInfoCache)
        {
            _multiChainBlockInfoCache = multiChainBlockInfoCache;
        }

        public IBlockInfo TryTake(int crossChainId, ulong height, bool isCacheSizeLimited)
        {
            var blockInfoCache = _multiChainBlockInfoCache.GetBlockInfoCache(crossChainId);
            if (blockInfoCache == null)
                return null;
            return blockInfoCache.TryTake(height, out var blockInfo, isCacheSizeLimited) ? blockInfo : null;
        }
    }
}