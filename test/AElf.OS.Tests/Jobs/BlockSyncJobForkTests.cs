using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution;
using Shouldly;
using Xunit;

namespace AElf.OS.Jobs
{
    public sealed class BlockSyncJobForkTests : SyncForkTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly BlockSyncJob _job;
        private readonly ITaskQueueManager _taskQueueManager;

        public BlockSyncJobForkTests()
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

        private void DisposeQueue()
        {
            _taskQueueManager.GetQueue(KernelConstants.UpdateChainQueueName).Dispose();
        }
    }
}