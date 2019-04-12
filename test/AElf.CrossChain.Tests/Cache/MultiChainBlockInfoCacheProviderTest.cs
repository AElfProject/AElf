using Xunit;

namespace AElf.CrossChain.Cache
{
    public class MultiChainBlockInfoCacheProviderTest : CrossChainTestBase
    {

        private readonly IMultiChainBlockInfoCacheProvider _multiChainBlockInfoCacheProvider;

        public MultiChainBlockInfoCacheProviderTest()
        {
            _multiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
        }
        
        [Fact]
        public void TryAdd_NULL()
        {
            int chainId = 123;
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, null);
            Assert.True(_multiChainBlockInfoCacheProvider.Size == 0);
        }
        
        [Fact]
        public void TryAdd_New()
        {
            int chainId = 123;
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, new BlockInfoCache(1));
            Assert.True(_multiChainBlockInfoCacheProvider.Size == 1);
        }
        
        [Fact]
        public void TryGet_Null()
        {
            int chainId = 123;
            
            var actualBlockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Null(actualBlockInfoCache);
        }
        
        [Fact]
        public void TryGet()
        {
            int chainId = 123;
            
            var blockInfoCache = new BlockInfoCache(1);
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);

            var actualBlockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_SameValue()
        {
            int chainId = 123;
            
            var blockInfoCache = new BlockInfoCache(1);
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);
            Assert.True(_multiChainBlockInfoCacheProvider.Size == 1);
            var actualBlockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache, actualBlockInfoCache);
        }
        
        [Fact]
        public void TryAdd_Twice_With_SameChainId_NotSameValue()
        {
            int chainId = 123;
            
            var blockInfoCache1 = new BlockInfoCache(1);
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache1);
            var blockInfoCache2 = new BlockInfoCache(2);
            _multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache2);
            Assert.True(_multiChainBlockInfoCacheProvider.Size == 1);
            var actualBlockInfoCache = _multiChainBlockInfoCacheProvider.GetBlockInfoCache(chainId);
            Assert.Equal(blockInfoCache2, actualBlockInfoCache);
        }
    }
}