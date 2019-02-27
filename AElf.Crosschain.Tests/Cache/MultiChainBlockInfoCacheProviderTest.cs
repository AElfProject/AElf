using AElf.CrossChain;
using Xunit;

namespace AElf.Crosschain
{
    public class MultiChainBlockInfoCacheProviderTest
    {
        [Fact]
        public void TryAdd_NULL()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, null);
            Assert.True(multiChainBlockInfoCacheProvider.Size == 0);
        }
        
        [Fact]
        public void TryAdd_New()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, new BlockInfoCache(1));
            Assert.True(multiChainBlockInfoCacheProvider.Size == 1);
        }
        
        [Fact]
        public void TryGet_Null()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            var actualBlockInfoCache = multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Null(actualBlockInfoCache);
        }
        
        [Fact]
        public void TryGet()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            var blockInfoCache = new BlockInfoCache(1);
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);

            var actualBlockInfoCache = multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_SameValue()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            var blockInfoCache = new BlockInfoCache(1);
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);
            Assert.True(multiChainBlockInfoCacheProvider.Size == 1);
            var actualBlockInfoCache = multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_NotSameValue()
        {
            int chainId = 123;
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            var blockInfoCache1 = new BlockInfoCache(1);
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache1);
            var blockInfoCache2 = new BlockInfoCache(2);
            multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache2);
            Assert.True(multiChainBlockInfoCacheProvider.Size == 1);
            var actualBlockInfoCache = multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache2, actualBlockInfoCache);
        }
    }
}