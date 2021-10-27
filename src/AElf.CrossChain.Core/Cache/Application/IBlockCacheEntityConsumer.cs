using AElf.CrossChain.Cache.Infrastructure;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface IBlockCacheEntityConsumer
    {
        T Take<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new();
    }
    
    public class BlockCacheEntityConsumer : IBlockCacheEntityConsumer, ITransientDependency
    {
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public BlockCacheEntityConsumer(ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public T Take<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new()
        {
            if (!_crossChainCacheEntityProvider.TryGetChainCacheEntity(crossChainId, out var chainCacheEntity))
            {
                return default(T);
            }

            if (!chainCacheEntity.TryTake(height, out var blockCacheEntity, isCacheSizeLimited))
            {
                return default(T);
            }

            var t = new T();
            t.MergeFrom(blockCacheEntity.ToByteString());
            return t;
        }
    }
}