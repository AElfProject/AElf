using System.Threading.Tasks;
using AElf.CrossChain.Application;
using AElf.CrossChain.Cache.Application;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainService _crossChainService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
    
        public CrossChainServiceTest()
        {
            _crossChainService = GetRequiredService<ICrossChainService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainCacheEntityService = GetRequiredService<ICrossChainCacheEntityService>();
        }
    
        [Fact]
        public async Task FinishInitialSync_Test()
        {
            int chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            long libHeight = 10;
            _crossChainTestHelper.AddFakeChainIdHeight(chainId,libHeight);
            await _crossChainService.FinishInitialSyncAsync();
            
            var height = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            Assert.Equal(libHeight + 1, height);
        }
    }
}