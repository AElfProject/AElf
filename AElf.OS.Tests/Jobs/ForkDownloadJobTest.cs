using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.Jobs
{
    public sealed class ForkDownloadJobTest : NetWorkTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly ForkDownloadJob _job;

        public ForkDownloadJobTest()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _job = GetRequiredService<ForkDownloadJob>();
        }

        [Fact]
        public async Task ExecSyncJob_ShouldSyncChain()
        {
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 12 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }
        
        [Fact]
        public async Task ExecSyncJob_QueryTooMuch_ShouldSyncChain()
        {
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 25 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(21);
        }
        
        [Fact]
        public async Task ExecSyncJob_RexecutionOfJob_ShouldNotChangeHeight()
        {
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 3 });
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 3 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
        
        [Fact]
        public async Task ExecSyncJob_Overlapping_ShouldSyncAllBlocks()
        {
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 12 });
            _job.Execute(new ForkDownloadJobArgs { BlockHeight = 15 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }
    }
}