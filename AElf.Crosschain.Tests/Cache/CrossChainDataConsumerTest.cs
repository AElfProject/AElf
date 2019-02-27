using System.Collections.Generic;
using AElf.CrossChain;
using Xunit;

namespace AElf.Crosschain
{
    public class CrossChainDataConsumerTest
    {
        private CrossChainDataConsumer CreateCrossChainDataConsumer(Dictionary<int, BlockInfoCache> cachingData)
        {
            var cacheProvider = CreateMultiChainBlockInfoCacheProvider(cachingData);
            return new CrossChainDataConsumer(cacheProvider);
        }

        private MultiChainBlockInfoCacheProvider CreateMultiChainBlockInfoCacheProvider(
            Dictionary<int, BlockInfoCache> cachingData)
        {
            var cacheProvider = new MultiChainBlockInfoCacheProvider();
            foreach (var kv in cachingData)
            {
                cachingData.Add(kv.Key, kv.Value);
            }

            return cacheProvider;
        }
        
        [Fact]
        public void TryTake_EmptyCache()
        {
            var consumer = CreateCrossChainDataConsumer(new Dictionary<int, BlockInfoCache>());
            int chainId = 123;
            var blockInfo = consumer.TryTake(chainId, 1, false);
            Assert.Null(blockInfo);
        }
        
        [Fact]
    }
}