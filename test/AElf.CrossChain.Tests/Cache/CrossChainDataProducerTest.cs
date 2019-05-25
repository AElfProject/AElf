using System;
using System.Collections.Generic;
using Acs7;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducerTest : CrossChainTestBase
    {
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        public CrossChainDataProducerTest()
        {
            _blockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
        }
        
        [Fact]
        public void TryAdd_Null()
        {
            Assert.Throws<ArgumentNullException>(() => _blockCacheEntityProducer.TryAddBlockCacheEntity(null));
        }

        [Fact]
        public void TryAdd_NotExistChain()
        {
            int chainId = 123;
            var res = _blockCacheEntityProducer.TryAddBlockCacheEntity(new SideChainBlockData
            {
                ChainId = chainId
            });
            Assert.False(res);
        }
        
        [Fact]
        public void TryAdd_ExistChain_WrongIndex()
        {
            int chainId = 123;
            var dict = new Dictionary<int, ChainCacheEntity>
            {
                {
                    chainId, new ChainCacheEntity(1)
                }
            };
            CreateFakeCache(dict);
            var res = _blockCacheEntityProducer.TryAddBlockCacheEntity(new SideChainBlockData
            {
                ChainId = chainId,
                Height = 2
            });
            Assert.False(res);
        }
        
        [Fact]
        public void TryAdd_ExistChain_CorrectIndex()
        {
            int chainId = 123;
            var dict = new Dictionary<int, ChainCacheEntity>
            {
                {
                    chainId, new ChainCacheEntity(1)
                }
            };
            CreateFakeCache(dict);
            var res = _blockCacheEntityProducer.TryAddBlockCacheEntity(new SideChainBlockData
            {
                ChainId = chainId,
                Height = 1
            });
            Assert.True(res);
        }
    }
}