using System;
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

        public bool TryAddBlockCacheEntity(BlockCacheEntity blockCacheEntity)
        {
            if (blockCacheEntity == null)
                throw new ArgumentNullException(nameof(blockCacheEntity));
            var chainCacheEntity = _chainCacheEntityProvider.GetChainCacheEntity(blockCacheEntity.ChainId);

            if (chainCacheEntity == null)
                return false;
            var res = chainCacheEntity.TryAdd(blockCacheEntity);
            return res;
        }
    }
}