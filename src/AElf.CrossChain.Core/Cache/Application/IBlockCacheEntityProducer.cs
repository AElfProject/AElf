using System;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface IBlockCacheEntityProducer
    {
        bool TryAddBlockCacheEntity(IBlockCacheEntity blockCacheEntity);
        ILogger<BlockCacheEntityProducer> Logger { get; set; }
    }
    
    public class BlockCacheEntityProducer : IBlockCacheEntityProducer, ISingletonDependency
    {
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public ILogger<BlockCacheEntityProducer> Logger { get; set; }
        
        public BlockCacheEntityProducer(ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public bool TryAddBlockCacheEntity(IBlockCacheEntity blockCacheEntity)
        {
            if (blockCacheEntity == null)
                throw new ArgumentNullException(nameof(blockCacheEntity));
            var chainCacheEntity = _crossChainCacheEntityProvider.GetChainCacheEntity(blockCacheEntity.ChainId);

            if (chainCacheEntity == null)
                return false;
            var res = chainCacheEntity.TryAdd(blockCacheEntity);
            return res;
        }
    }
}