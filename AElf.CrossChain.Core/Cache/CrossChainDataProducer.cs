namespace AElf.CrossChain
{
    public class CrossChainDataProducer : ICrossChainDataProducer
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public CrossChainDataProducer(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            _multiChainBlockInfoCacheProvider = multiChainBlockInfoCacheProvider;
        }

        public bool AddNewBlockInfo(IBlockInfo blockInfo)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(blockInfo.ChainId);

            if (blockInfo.Height != blockInfoCache.TargetChainHeight)
                return false;
            var res = blockInfoCache.TryAdd(blockInfo);
            return res;
        }

        public ulong GetChainHeightNeededForCache(int chainId)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            if (blockInfoCache == null)
                return 0;
            return blockInfoCache.TargetChainHeight;
        }
    }
}