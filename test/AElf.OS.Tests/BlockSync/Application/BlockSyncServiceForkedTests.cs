using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public sealed class BlockSyncServiceForkedTests : SyncForkedTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly IBlockSyncService _blockSyncService;
        private readonly ITaskQueueManager _taskQueueManager;

        public BlockSyncServiceForkedTests()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _blockSyncService = GetRequiredService<IBlockSyncService>();
            _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        }

        [Fact]
        public async Task SyncBlock_ShouldSyncChain()
        {
            await _blockSyncService.SyncBlockAsync(null, 12, 10, null);

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