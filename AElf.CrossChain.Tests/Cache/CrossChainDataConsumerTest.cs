using System.Collections.Generic;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest : CrossChainTestBase
    {
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;

        public CrossChainDataConsumerTest()
        {
            _crossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
        }
        
        [Fact]
        public void TryTake_EmptyCache()
        {
            int chainId = 123;
            var blockInfo = _crossChainDataConsumer.TryTake(chainId, 1, false);
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
            CreateFakeCache(dict);
            int chainIdB = 124;
            var blockInfo = _crossChainDataConsumer.TryTake(chainIdB, 1, false);
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
            CreateFakeCache(dict);
            var blockInfo = _crossChainDataConsumer.TryTake(chainId, 2, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void CachedCount_Empty()
        {
            var count = _crossChainDataConsumer.GetCachedChainCount();
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
            CreateFakeCache(dict);
            var count = _crossChainDataConsumer.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void RegisterNewChain_NotNull()
        {
            int chainId = 123;
            _crossChainDataConsumer.RegisterNewChainCache(chainId, 1);
            var count = _crossChainDataConsumer.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void TryTake_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainDataConsumer.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = MultiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(expectedBlockInfo);
            var actualBlockInfo = _crossChainDataConsumer.TryTake(chainId, 1, false);
            Assert.Equal(expectedBlockInfo, actualBlockInfo);
        }
        
        [Fact]
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainDataConsumer.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = MultiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(expectedBlockInfo);
            var blockInfo = _crossChainDataConsumer.TryTake(chainId, 2, false);
            Assert.Null(blockInfo);
        }
    }
}