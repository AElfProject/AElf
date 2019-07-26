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
        public async Task ResponseSideChainBlockData()
        {
            var res = await _chainResponseService.ResponseSideChainBlockDataAsync(2);
            Assert.NotNull(res);
        }

        [Fact]
        public async Task ResponseParentChainBlockData()
        {
            var chainId = 123;
            var height = 2;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            Assert.NotNull(res);
        }
    }
}