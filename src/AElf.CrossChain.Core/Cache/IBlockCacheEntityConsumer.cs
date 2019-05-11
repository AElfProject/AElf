using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IBlockCacheEntityConsumer
    {
        T Take<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new();
    }
    
    public class BlockCacheEntityConsumer : IBlockCacheEntityConsumer, ITransientDependency
    {
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public BlockCacheEntityConsumer(IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }

        public T Take<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new()
        {
            var chainCacheCollection = _chainCacheEntityProvider.GetChainCacheEntity(crossChainId);
            if (chainCacheCollection == null || !chainCacheCollection.TryTake(height, out var blockCacheEntity, isCacheSizeLimited))
                return default(T);
            var t = new T();
            t.MergeFrom(blockCacheEntity.Payload);
            return t;
        }
    }
}