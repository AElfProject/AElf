using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainResponseServiceTest : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainResponseService _chainResponseService;

        public CrossChainResponseServiceTest()
        {
            _chainResponseService = GetRequiredService<ICrossChainResponseService>();
        }

        [Fact]
        public async Task ResponseSideChainBlockData_Test()
        {
            var res = await _chainResponseService.ResponseSideChainBlockDataAsync(2);
            Assert.NotNull(res);
        }

        [Fact]
        public async Task ResponseParentChainBlockData_WithoutBlock_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var height = 5;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            Assert.Null(res);
        }
        
        [Fact]
        public async Task ResponseParentChainBlockData_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var height = 3;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            Assert.NotNull(res);
        }
    }
}