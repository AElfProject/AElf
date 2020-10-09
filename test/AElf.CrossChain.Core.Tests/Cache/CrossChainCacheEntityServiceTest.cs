using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Cache.Application;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainCacheEntityServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;

        public CrossChainCacheEntityServiceTest()
        {
            _crossChainCacheEntityService = GetService<ICrossChainCacheEntityService>();
        }

        [Fact]
        public void RegisterNewChainTest()
        {
            var chainId = 1;
            _crossChainCacheEntityService.RegisterNewChain(chainId, 1);
            var cachedChainIdList = _crossChainCacheEntityService.GetCachedChainIds();
            cachedChainIdList.ShouldContain(chainId);

            _crossChainCacheEntityService.GetCachedChainIds();
            var targetHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            targetHeight.ShouldBe(2);
        }

        [Fact]
        public async Task UpdateCrossChainCacheAsyncTest()
        {
            var chainId = 1;
            _crossChainCacheEntityService.RegisterNewChain(chainId, 10);
            var cachedChainIdList = _crossChainCacheEntityService.GetCachedChainIds();
            cachedChainIdList.ShouldContain(chainId);

            await _crossChainCacheEntityService.UpdateCrossChainCacheAsync(null, 0, new ChainIdAndHeightDict
            {
                IdHeightDict = {{chainId, 12}}
            });
            var targetHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            targetHeight.ShouldBe(13);
        }
    }
}