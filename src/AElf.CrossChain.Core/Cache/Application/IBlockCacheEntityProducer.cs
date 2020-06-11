using System;
using AElf.CrossChain.Cache.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface IBlockCacheEntityProducer
    {
        bool TryAddBlockCacheEntity(ICrossChainBlockEntity crossChainBlockEntity);
    }
    
    public class BlockCacheEntityProducer : IBlockCacheEntityProducer, ISingletonDependency
    {
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public ILogger<BlockCacheEntityProducer> Logger { get; set; }
        
        public BlockCacheEntityProducer(ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public bool TryAddBlockCacheEntity(ICrossChainBlockEntity crossChainBlockEntity)
        {
            if (crossChainBlockEntity == null)
                throw new ArgumentNullException(nameof(crossChainBlockEntity));
            if (!_crossChainCacheEntityProvider.TryGetChainCacheEntity(crossChainBlockEntity.ChainId, out var chainCacheEntity))
            {
                return false;
            }

            var res = chainCacheEntity.TryAdd(crossChainBlockEntity);

            Logger.LogDebug(
                $"Cached height {crossChainBlockEntity.Height} from chain {ChainHelper.ConvertChainIdToBase58(crossChainBlockEntity.ChainId)}, {res}");
            return res;
        }
    }
}