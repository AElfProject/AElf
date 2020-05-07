using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class ChainBlockLinkServiceTests: AElfKernelTestBase
    {
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ChainBlockLinkServiceTests()
        {
            _chainBlockLinkService = GetRequiredService<IChainBlockLinkService>();
        }

        [Fact]
        private void GetCachedChainBlockLinks_Tests()
        {
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks();
            chainBlockLinks.Count.ShouldBe(0);
        }

        [Fact]
        private async Task GetNotExecutedChainBlockLinksAsync_Tests()
        {
            var l = await _chainBlockLinkService.GetNotExecutedChainBlockLinksAsync(HashHelper.ComputeFrom("test"));
                l.Count.ShouldBe(0);
        }

        [Fact]
        private void CleanCachedChainBlockLinks_Tests()
        {
            _chainBlockLinkService.CleanCachedChainBlockLinks(long.MaxValue);
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks();
            chainBlockLinks.Count.ShouldBe(0);
        }

        [Fact]
        private async Task SetChainBlockLinkExecutionStatusAsync_Tests()
        {
            
        }
    }
}