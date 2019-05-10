using System;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class ChainCacheEntityProviderTest : CrossChainTestBase
    {
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public ChainCacheEntityProviderTest()
        {
            _chainCacheEntityProvider = GetRequiredService<IChainCacheEntityProvider>();
        }
        
        [Fact]
        public void TryAdd_NULL()
        {
            int chainId = 123;
            Assert.Throws<ArgumentNullException>(() => _chainCacheEntityProvider.AddChainCacheEntity(chainId, null));
        }
        
        [Fact]
        public void TryAdd_New()
        {
            int chainId = 123;
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, new ChainCacheEntity(1));
            Assert.True(_chainCacheEntityProvider.Size == 1);
        }
        
        [Fact]
        public void TryGet_Null()
        {
            int chainId = 123;
            
            var actualBlockInfoCache = _chainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.Null(actualBlockInfoCache);
        }
        
        [Fact]
        public void TryGet()
        {
            int chainId = 123;
            
            var blockInfoCache = new ChainCacheEntity(1);
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, blockInfoCache);

            var actualBlockInfoCache = _chainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_SameValue()
        {
            int chainId = 123;
            
            var blockInfoCache = new ChainCacheEntity(1);
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, blockInfoCache);
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, blockInfoCache);
            Assert.True(_chainCacheEntityProvider.Size == 1);
            var actualBlockInfoCache = _chainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_NotSameValue()
        {
            int chainId = 123;
            
            var blockInfoCache1 = new ChainCacheEntity(1);
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, blockInfoCache1);
            var blockInfoCache2 = new ChainCacheEntity(2);
            _chainCacheEntityProvider.AddChainCacheEntity(chainId, blockInfoCache2);
            Assert.True(_chainCacheEntityProvider.Size == 1);
            var actualBlockInfoCache = _chainCacheEntityProvider.GetChainCacheEntity(chainId);
            Assert.Equal(blockInfoCache1, actualBlockInfoCache);
        }
    }
}