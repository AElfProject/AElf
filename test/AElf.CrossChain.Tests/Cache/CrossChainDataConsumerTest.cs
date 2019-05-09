using System.Collections.Generic;
using AElf.Contracts.CrossChain;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest : CrossChainTestBase
    {
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        private readonly ICrossChainMemoryCacheService _crossChainMemoryCacheService;

        public CrossChainDataConsumerTest()
        {
            _crossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
            _crossChainMemoryCacheService = GetRequiredService<ICrossChainMemoryCacheService>();
        }
        
        [Fact]
        public void TryTake_EmptyCache()
        {
            int chainId = 123;
            var blockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(chainId, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_NotExistChain()
        {
            int chainIdA = 123;
            var dict = new Dictionary<int, CrossChainCacheCollection>
            {
                {chainIdA, new CrossChainCacheCollection(1)}
            };
            CreateFakeCache(dict);
            int chainIdB = 124;
            var blockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(chainIdB, 1, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_WrongIndex()
        {
            int chainId = 123;
            var blockInfoCache = new CrossChainCacheCollection(1);
            var sideChainBlockData = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new CrossChainCacheData
            {
                ChainId = sideChainBlockData.SideChainId,
                Height = sideChainBlockData.SideChainHeight,
                Payload = sideChainBlockData.ToByteString()
            });
            var dict = new Dictionary<int, CrossChainCacheCollection>
            {
                {chainId, blockInfoCache}
            };
            CreateFakeCache(dict);
            var blockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(chainId, 2, false);
            Assert.Null(blockInfo);
        }

        [Fact]
        public void TryTake_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainMemoryCacheService.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = MultiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new CrossChainCacheData
            {
                ChainId = expectedBlockInfo.SideChainId,
                Height = expectedBlockInfo.SideChainHeight,
                Payload = expectedBlockInfo.ToByteString()
            });
            var actualBlockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(chainId, 1, false);
            Assert.Equal(expectedBlockInfo, actualBlockInfo);
        }
        
        [Fact]
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            int chainId = 123;
            _crossChainMemoryCacheService.RegisterNewChainCache(chainId, 1);
            var blockInfoCache = MultiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new CrossChainCacheData
            {
                ChainId = expectedBlockInfo.SideChainId,
                Height = expectedBlockInfo.SideChainHeight,
                Payload = expectedBlockInfo.ToByteString()
            });
            var blockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(chainId, 2, false);
            Assert.Null(blockInfo);
        }
    }
}