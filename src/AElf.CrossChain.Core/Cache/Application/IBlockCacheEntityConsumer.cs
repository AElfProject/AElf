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
            var chainCacheCollection = _crossChainCacheEntityProvider.GetChainCacheEntity(crossChainId);
            if (chainCacheCollection == null || !chainCacheCollection.TryTake(height, out var blockCacheEntity, isCacheSizeLimited))
                return default(T);
            var t = new T();
            t.MergeFrom(blockCacheEntity.ToByteString());
            return t;
        }
    }
}