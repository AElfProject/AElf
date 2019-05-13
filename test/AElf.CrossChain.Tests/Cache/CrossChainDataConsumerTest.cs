using System.Collections.Generic;
using AElf.Contracts.CrossChain;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataConsumerTest : CrossChainTestBase
    {
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public CrossChainDataConsumerTest()
        {
            _blockCacheEntityConsumer = GetRequiredService<IBlockCacheEntityConsumer>();
            _chainCacheEntityProvider = GetRequiredService<IChainCacheEntityProvider>();
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
            var dict = new Dictionary<int, ChainCacheEntity>
            {
                {chainIdA, new ChainCacheEntity(1)}
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
            var blockInfoCache = new ChainCacheEntity(1);
            var sideChainBlockData = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new BlockCacheEntity
            {
                ChainId = sideChainBlockData.SideChainId,
                Height = sideChainBlockData.SideChainHeight,
                Payload = sideChainBlockData.ToByteString()
            });
            var dict = new Dictionary<int, ChainCacheEntity>
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
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, new ChainCacheEntity(1));
            var blockInfoCache = ChainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new BlockCacheEntity
            {
                ChainId = expectedBlockInfo.SideChainId,
                Height = expectedBlockInfo.SideChainHeight,
                Payload = expectedBlockInfo.ToByteString()
            });
            var actualBlockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 1, false);
            Assert.Equal(expectedBlockInfo, actualBlockInfo);
        }
        
        [Fact]
        public void TryTake_WrongIndex_After_RegisterNewChain()
        {
            int chainId = 123;
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, new ChainCacheEntity(1));
            var blockInfoCache = ChainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.NotNull(blockInfoCache);
            var expectedBlockInfo = new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            };
            blockInfoCache.TryAdd(new BlockCacheEntity
            {
                ChainId = expectedBlockInfo.SideChainId,
                Height = expectedBlockInfo.SideChainHeight,
                Payload = expectedBlockInfo.ToByteString()
            });
            var blockInfo = _blockCacheEntityConsumer.Take<SideChainBlockData>(chainId, 2, false);
            Assert.Null(blockInfo);
        }
    }
}