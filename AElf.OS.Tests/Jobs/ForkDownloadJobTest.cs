using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.Jobs
{
    public sealed class ForkDownloadJobTest : NetWorkTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly BlockSyncJob _job;

        public ForkDownloadJobTest()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _job = GetRequiredService<BlockSyncJob>();
        }

        [Fact]
        public async Task ExecSyncJob_ShouldSyncChain()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 12});

            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_QueryTooMuch_ShouldSyncChain()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 25});

            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_RexecutionOfJob_ShouldNotChangeHeight()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 3});
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 3});

            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_Overlapping_ShouldSyncAllBlocks()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 12});
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 15});

            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }
    }
}