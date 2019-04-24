using System.Collections.Generic;
using AElf.CrossChain.Cache.Exception;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainMemCacheServiceTest :CrossChainTestBase
    {
        private readonly ICrossChainMemoryCacheService _crossChainMemoryCacheService;

        public CrossChainMemCacheServiceTest()
        {
            _crossChainMemoryCacheService = GetRequiredService<ICrossChainMemoryCacheService>();
        }

        [Fact]
        public void CachedCount_Empty()
        {
            var count = _crossChainMemoryCacheService.GetCachedChainCount();
            Assert.True(0 == count);
        }
        
        [Fact]
        public void CachedCount_NotEmpty()
        {
            int chainId = 123;
            var dict = new Dictionary<int, BlockInfoCache>
            {
                {chainId, new BlockInfoCache(1)}
            };
            CreateFakeCache(dict);
            var count = _crossChainMemoryCacheService.GetCachedChainCount();
            Assert.True(1 == count);
        }

        [Fact]
        public void RegisterNewChain_NotNull()
        {
            int chainId = 123;
            _crossChainMemoryCacheService.TryRegisterNewChainCache(chainId, 1);
            var count = _crossChainMemoryCacheService.GetCachedChainCount();
            Assert.True(1 == count);
        }
        
        [Fact]
        public void GetChainHeightNeeded_NotExistChain()
        {
            int chainId = 123;
            Assert.Throws<ChainCacheNotFoundException>(() => _crossChainMemoryCacheService.GetChainHeightNeeded(chainId));
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
            var neededHeight = _crossChainMemoryCacheService.GetChainHeightNeeded(chainId);
            Assert.True(neededHeight == 1);
        }
    }
}