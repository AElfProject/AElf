using System.Collections.Generic;
using AElf.CrossChain.Cache.Exception;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducerTest : CrossChainTestBase
    {
        public CrossChainDataProducerTest()
        {
            _crossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
        }

        private readonly ICrossChainDataProducer _crossChainDataProducer;

        [Fact]
        public void GetChainHeightNeeded_ExistChain()
        {
            var chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {
                    chainId, new BlockInfoCache(1)
                }
            };
            CreateFakeCache(dict);
            var neededHeight = _crossChainDataProducer.GetChainHeightNeeded(chainId);
            Assert.True(neededHeight == 1);
        }

        [Fact]
        public void GetChainHeightNeeded_NotExistChain()
        {
            var chainId = 123;
            Assert.Throws<ChainCacheNotFoundException>(() => _crossChainDataProducer.GetChainHeightNeeded(chainId));
        }

        [Fact]
        public void TryAdd_ExistChain_CorrectIndex()
        {
            var chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {
                    chainId, new BlockInfoCache(1)
                }
            };
            CreateFakeCache(dict);
            var res = _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            });
            Assert.True(res);
        }

        [Fact]
        public void TryAdd_ExistChain_WrongIndex()
        {
            var chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {
                    chainId, new BlockInfoCache(1)
                }
            };
            CreateFakeCache(dict);
            var res = _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 2
            });
            Assert.False(res);
        }

        [Fact]
        public void TryAdd_NotExistChain()
        {
            var chainId = 123;
            var res = _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
            {
                SideChainId = chainId
            });
            Assert.False(res);
        }

        [Fact]
        public void TryAdd_Null()
        {
            var res = _crossChainDataProducer.AddNewBlockInfo(null);
            Assert.False(res);
        }
    }
}