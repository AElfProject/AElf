using System;

namespace AElf.CrossChain.Cache
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
            if (blockInfo == null)
                return false;
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(blockInfo.ChainId);

            if (blockInfoCache == null)
                return false;
            var res = blockInfoCache.TryAdd(blockInfo);
            return res;
        }

        public ulong GetChainHeightNeededForCache(int chainId)
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            if (blockInfoCache == null)
                throw new Exception("Chain data cache not found.");
            return blockInfoCache.TargetChainHeight;
        }
    }
}