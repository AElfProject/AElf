using System;
using System.Collections.Generic;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducerTest : CrossChainTestBase
    {
        private readonly ICrossChainDataProducer _crossChainDataProducer;

        public CrossChainDataProducerTest()
        {
            _crossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
        }
        
        [Fact]
        public void GetChainHeightNeeded_NotExistChain()
        {
            int chainId = 123;
            Assert.Throws<Exception>(() => _crossChainDataProducer.GetChainHeightNeededForCache(chainId));
        }
        
        [Fact]
        public void GetChainHeightNeeded_ExistChain()
        {
            int chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {
                    chainId, new BlockInfoCache(1)
                }
            };
            CreateFakeCache(dict);
            var neededHeight = _crossChainDataProducer.GetChainHeightNeededForCache(chainId);
            Assert.True(neededHeight == 1);
        }
        
        [Fact]
        public void TryAdd_Null()
        {
            var res = _crossChainDataProducer.AddNewBlockInfo(null);
            Assert.False(res);
        }

        [Fact]
        public void TryAdd_NotExistChain()
        {
            int chainId = 123;
            var res = _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
            {
                SideChainId = chainId
            });
            Assert.False(res);
        }
        
        [Fact]
        public void TryAdd_ExistChain_WrongIndex()
        {
            int chainId = 123;
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
        public void TryAdd_ExistChain_CorrectIndex()
        {
            int chainId = 123;
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
    }
}