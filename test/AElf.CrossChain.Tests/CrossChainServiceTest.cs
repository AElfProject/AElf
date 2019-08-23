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
        public async Task FinishInitialSync_Test()
        {
            int chainId = _chainOptions.ChainId;
            long libHeight = 10;
            _crossChainTestHelper.SetFakeLibHeight(libHeight);
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId,libHeight);
            await _crossChainService.FinishInitialSyncAsync();
            
            var info = _crossChainService.GetNeededChainIdAndHeightPairs();
            Assert.Contains(chainId,info.Keys);
            Assert.Contains(libHeight + 1, info.Values);
        }
        
        [Fact]
        public async Task FinishInitialSync_NotReady_Test()
        {
            await _crossChainService.FinishInitialSyncAsync();
            var info = _crossChainService.GetNeededChainIdAndHeightPairs();
            Assert.Empty(info);
        }
        
        [Fact]
        public async Task GetNonIndexedBlock_Test()
        {
            _crossChainTestHelper.SetFakeLibHeight(2);
            var res = await _crossChainService.GetNonIndexedBlockAsync(1);
            Assert.True(res.Height.Equals(1));
        }
        
        [Fact]
        public async Task GetNonIndexedBlock_NoBlock_Test()
        { 
            _crossChainTestHelper.SetFakeLibHeight(1);
            var res = await _crossChainService.GetNonIndexedBlockAsync(2);
            Assert.True(res == null);
        }
    }
}