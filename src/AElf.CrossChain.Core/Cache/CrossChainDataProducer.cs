using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducer : ICrossChainDataProducer, ISingletonDependency
    {
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public ILogger<CrossChainDataProducer> Logger { get; set; }
        
        public CrossChainDataProducer(IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }

        public bool AddCacheEntity(BlockCacheEntity blockCacheEntity)
        {
            if (blockCacheEntity == null)
                return false;
            var blockInfoCache = _chainCacheEntityProvider.GetBlockInfoCache(blockCacheEntity.ChainId);

            if (blockInfoCache == null)
                return false;
            var res = blockInfoCache.TryAdd(blockCacheEntity);
            return res;
        }
    }
}