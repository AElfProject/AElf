using System;
using System.Collections.Generic;
using AElf.CrossChain.Cache.Exception;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducerTest : CrossChainTestBase
    {
        private readonly ICrossChainDataProducer _crossChainDataProducer;

        public CrossChainDataProducerTest()
        {
            _crossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            _crossChainMemCacheService = GetRequiredService<ICrossChainMemCacheService>();
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