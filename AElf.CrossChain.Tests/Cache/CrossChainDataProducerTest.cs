using System;
using System.Collections.Generic;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainDataProducerTest
    {
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
        
        private CrossChainDataProducer CreateCrossChainDataProducer(Dictionary<int, BlockInfoCache> cachingData)
        {
            var cacheProvider = CreateFakeMultiChainBlockInfoCacheProvider(cachingData);
            return new CrossChainDataProducer(cacheProvider);
        }

        [Fact]
        public void GetChainHeightNeeded_NotExistChain()
        {
            var producer = CreateCrossChainDataProducer(new Dictionary<int, BlockInfoCache>());
            int chainId = 123;
            Assert.Throws<Exception>(() => producer.GetChainHeightNeededForCache(chainId));
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
            var consumer = CreateCrossChainDataProducer(dict);
            var neededHeight = consumer.GetChainHeightNeededForCache(chainId);
            Assert.True(neededHeight == 1);
        }
        
        [Fact]
        public void TryAdd_Null()
        {
            var producer = CreateCrossChainDataProducer(new Dictionary<int, BlockInfoCache>());
            var res = producer.AddNewBlockInfo(null);
            Assert.False(res);
        }

        [Fact]
        public void TryAdd_NotExistChain()
        {
            var producer = CreateCrossChainDataProducer(new Dictionary<int, BlockInfoCache>());
            int chainId = 123;
            var res = producer.AddNewBlockInfo(new SideChainBlockData
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
            var producer = CreateCrossChainDataProducer(dict);
            var res = producer.AddNewBlockInfo(new SideChainBlockData
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
            var producer = CreateCrossChainDataProducer(dict);
            var res = producer.AddNewBlockInfo(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            });
            Assert.True(res);
        }
    }
}