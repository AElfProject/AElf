using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution;
using Shouldly;
using Xunit;

namespace AElf.OS.Jobs
{
    public sealed class ForkDownloadJobTest : NetWorkTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly BlockSyncJob _job;
        private readonly ITaskQueueManager _taskQueueManager;

        public ForkDownloadJobTest()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _job = GetRequiredService<BlockSyncJob>();
            _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        }

        [Fact]
        public async Task ExecSyncJob_ShouldSyncChain()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 12});

            DisposeQueue();
                
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_QueryTooMuch_ShouldSyncChain()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 25});
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_RexecutionOfJob_ShouldNotChangeHeight()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 3});
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 3});
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        [Fact]
        public async Task ExecSyncJob_Overlapping_ShouldSyncAllBlocks()
        {
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 12});
            await _job.ExecuteAsync(new BlockSyncJobArgs {BlockHeight = 15});
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(15);
        }

        private void DisposeQueue()
        {
            _taskQueueManager.GetQueue(ExecutionConsts.BlockAttachQueueName).Dispose();
        }
    }
}