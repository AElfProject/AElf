using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumer : ICrossChainDataConsumer, ITransientDependency
    {
        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public CrossChainDataConsumer(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            _multiChainBlockInfoCacheProvider = multiChainBlockInfoCacheProvider;
        }

        public T TryTake<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new()
        {
            var blockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(crossChainId);
            if (blockInfoCache == null || !blockInfoCache.TryTake(height, out var blockInfo, isCacheSizeLimited))
                return default(T);
            var t = new T();
            t.MergeFrom(blockInfo.Payload);
            return t;
        }
    }
}