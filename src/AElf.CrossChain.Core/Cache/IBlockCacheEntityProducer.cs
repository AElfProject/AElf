using System;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IBlockCacheEntityProducer
    {
        bool TryAddBlockCacheEntity(IBlockCacheEntity blockCacheEntity);
        ILogger<BlockCacheEntityProducer> Logger { get; set; }
    }
    
    public class BlockCacheEntityProducer : IBlockCacheEntityProducer, ISingletonDependency
    {
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public ILogger<BlockCacheEntityProducer> Logger { get; set; }
        
        public BlockCacheEntityProducer(IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }

        public bool TryAddBlockCacheEntity(IBlockCacheEntity blockCacheEntity)
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