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
    }
}