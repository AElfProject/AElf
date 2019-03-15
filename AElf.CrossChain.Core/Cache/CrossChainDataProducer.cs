using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.CrossChain.Cache.Exception;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducer : ICrossChainDataProducer, ISingletonDependency
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public ILogger<CrossChainDataProducer> Logger { get; set; }
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