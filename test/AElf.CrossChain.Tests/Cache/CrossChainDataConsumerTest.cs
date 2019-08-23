using System.Collections.Generic;
using Acs7;
using AElf.CrossChain.Cache.Application;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest : CrossChainTestBase
    {
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public CrossChainDataConsumerTest()
        {
            _blockCacheEntityConsumer = GetRequiredService<IBlockCacheEntityConsumer>();
            _crossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
        }
        
        [Fact]
        public void TryTake_EmptyCache()
        {
            int chainId = 123;
            var blockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_NotExistChain()
        {
            int chainIdA = 123;
            var dict = new Dictionary<int, BlockCacheEntityProvider>
            {
                {chainIdA, new BlockCacheEntityProvider(1)}
            };
            CreateFakeCache(dict);
            int chainIdB = 124;
            var blockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainIdB, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_WrongIndex()
        {
            int chainId = 123;
            var blockInfoCache = new BlockCacheEntityProvider(1);
            var sideChainBlockData = CreateSideChainBlockData(chainId, 1);
            blockInfoCache.TryAdd(sideChainBlockData);
            var dict = new Dictionary<int, BlockCacheEntityProvider>
            {
                {chainId, blockInfoCache}
            };
            CreateFakeCache(dict);
            var blockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 2, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 1);
            var blockInfoCache = CrossChainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = CreateSideChainBlockData(chainId, 1);
            blockInfoCache.TryAdd(expectedBlockInfo);
            var actualBlockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 1, false);
            Assert.Equal(expectedBlockInfo, actualBlockInfo);
        }
        
        [Fact]
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 1);
            var blockInfoCache = CrossChainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = CreateSideChainBlockData(chainId, 1);
            blockInfoCache.TryAdd(expectedBlockInfo);
            var blockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 2, false);
            Assert.Null(blockInfo);
        }

        private SideChainBlockData CreateSideChainBlockData(int chainId, long height)
        {
            return new SideChainBlockData
            {
                ChainId = chainId,
                Height = height
            };
        }
    }
}