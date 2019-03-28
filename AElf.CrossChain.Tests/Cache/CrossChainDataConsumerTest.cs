using System.Collections.Generic;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest : CrossChainTestBase
    {
        public CrossChainDataConsumerTest()
        {
            _crossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
        }

        private readonly ICrossChainDataConsumer _crossChainDataConsumer;

        [Fact]
        public void CachedCount_Empty()
        {
            var count = _crossChainDataConsumer.GetCachedChainCount();
            Assert.True(0 == count);
        }

        [Fact]
        public void CachedCount_NotEmpty()
        {
            var chainId = 123;
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
            var chainId = 123;
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId, 1);
            var count = _crossChainDataConsumer.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void TryTake_After_RegisterNewChain()
        {
            var chainId = 123;
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId, 1);
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
        public void TryTake_EmptyCache()
        {
            var chainId = 123;
            var blockInfo = _crossChainDataConsumer.TryTake(chainId, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_NotExistChain()
        {
            var chainIdA = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {chainIdA, new BlockInfoCache(1)}
            };
            CreateFakeCache(dict);
            var chainIdB = 124;
            var blockInfo = _crossChainDataConsumer.TryTake(chainIdB, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_WrongIndex()
        {
            var chainId = 123;
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
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            var chainId = 123;
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId, 1);
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