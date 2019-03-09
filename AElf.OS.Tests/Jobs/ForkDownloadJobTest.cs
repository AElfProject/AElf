using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJobTest : NetWorkTestBase
    {
        private IBlockchainService _blockChainService;
        private ForkDownloadJob _job;

        public ForkDownloadJobTest()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _job = GetRequiredService<ForkDownloadJob>();
        }

        [Fact]
        public async Task ExecSyncJob_ShouldSyncChain()
        {
            var initialState = await _blockChainService.GetChainAsync();
            var genHash = initialState.LongestChainHash;
            
            _job.Execute(new ForkDownloadJobArgs { BlockHash = genHash.ToHex(), BlockHeight = 3 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.LongestChainHeight.ShouldBe<long>(6);
        }
    }
}