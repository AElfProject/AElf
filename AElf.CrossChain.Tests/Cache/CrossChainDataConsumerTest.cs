using System.Collections.Generic;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest
    {
        private CrossChainDataConsumer CreateCrossChainDataConsumer(Dictionary<int, BlockInfoCache> cachingData)
        {
            var cacheProvider = CreateFakeMultiChainBlockInfoCacheProvider(cachingData);
            return new CrossChainDataConsumer(cacheProvider);
        }

        private MultiChainBlockInfoCacheProvider CreateFakeMultiChainBlockInfoCacheProvider(
            Dictionary<int, BlockInfoCache> cachingData)
        {
            var cacheProvider = new MultiChainBlockInfoCacheProvider();
            foreach (var (key, value) in cachingData)
            {
                cacheProvider.AddBlockInfoCache(key, value);
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
        public void TryTake_NotExistChain()
        {
            int chainIdA = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {chainIdA, new BlockInfoCache(1)}
            };
            var consumer = CreateCrossChainDataConsumer(dict);
            int chainIdB = 124;
            var blockInfo = consumer.TryTake(chainIdB, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_WrongIndex()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache(1);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            });
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {chainId, blockInfoCache}
            };
            var consumer = CreateCrossChainDataConsumer(dict);
            var blockInfo = consumer.TryTake(chainId, 2, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void CachedCount_Empty()
        {
            var consumer = CreateCrossChainDataConsumer(new Dictionary<int, BlockInfoCache>());
            var count = consumer.GetCachedChainCount();
            Assert.True(0 == count);
        }
        
        [Fact]
        public void CachedCount_NotEmpty()
        {
            int chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {chainId, new BlockInfoCache(1)}
            };
            var consumer = CreateCrossChainDataConsumer(dict);
            var count = consumer.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void RegisterNewChain_NotNull()
        {
            var consumer = CreateCrossChainDataConsumer(new Dictionary<int, BlockInfoCache>());
            int chainId = 123;
            consumer.RegisterNewChainCache(chainId, 1);
            var count = consumer.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void TryTake_After_RegisterNewChain()
        {
            var multiChainCacheProvider = new MultiChainBlockInfoCacheProvider();
            var consumer = new CrossChainDataConsumer(multiChainCacheProvider);
            int chainId = 123;
            consumer.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = multiChainCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(expectedBlockInfo);
            var actualBlockInfo = consumer.TryTake(chainId, 1, false);
            Assert.Equal(expectedBlockInfo, actualBlockInfo);
        }
        
        [Fact]
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            var multiChainCacheProvider = new MultiChainBlockInfoCacheProvider();
            var consumer = new CrossChainDataConsumer(multiChainCacheProvider);
            int chainId = 123;
            consumer.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = multiChainCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(expectedBlockInfo);
            var blockInfo = consumer.TryTake(chainId, 2, false);
            Assert.Null(blockInfo);
        }
    }
}