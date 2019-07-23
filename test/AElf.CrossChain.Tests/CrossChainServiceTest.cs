using System.Threading.Tasks;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainService _crossChainService;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainServiceTest()
        {
            _crossChainService = GetRequiredService<ICrossChainService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        [Fact]
        public async Task FinishInitialSync()
        {
            int chainId = 123;
            long libHeight = 10;
            _crossChainTestHelper.SetFakeLibHeight(libHeight);
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId,libHeight);
            await _crossChainService.FinishInitialSyncAsync();
            
            var info = _crossChainService.GetNeededChainIdAndHeightPairs();
            Assert.Contains(chainId,info.Keys);
            Assert.Contains(libHeight + 1, info.Values);
        }
        
        [Fact]
        public async Task GetNonIndexedBlock_NoBlock()
        { 
            _crossChainTestHelper.SetFakeLibHeight(1);
            var info = await _crossChainService.GetNonIndexedBlockAsync(2);
            Assert.True(info == null);
        }
    }
}