using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    // TODO: Need more test for block sync
    public sealed class BlockSyncServiceNotForkedTests : SyncNotForkedTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly IBlockSyncService _blockSyncService;
        private readonly ITaskQueueManager _taskQueueManager;

        public BlockSyncServiceNotForkedTests()
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
            chain.BestChainHeight.ShouldBe(21);
        }

        [Fact]
        public async Task SyncBlock_SyncTooMuch_ShouldSyncChain()
        {
            await _blockSyncService.SyncBlockAsync(null, 25, 10, null);
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(21);
        }

        [Fact]
        public async Task SyncBlock_ReSync_ShouldNotChangeHeight()
        {
            await _blockSyncService.SyncBlockAsync(null, 3, 10, null);
            await _blockSyncService.SyncBlockAsync(null, 3, 10, null);
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(21);
        }

        [Fact]
        public async Task SyncBlock_Overlapping_ShouldSyncAllBlocks()
        {
            await _blockSyncService.SyncBlockAsync(null, 12, 10, null);
            await _blockSyncService.SyncBlockAsync(null, 15, 10, null);
            
            DisposeQueue();
            
            var chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(21);
        }

        private void DisposeQueue()
        {
            _taskQueueManager.GetQueue(OSConsts.BlockSyncQueueName).Dispose();
            _taskQueueManager.GetQueue(OSConsts.BlockSyncAttachQueueName).Dispose();
            _taskQueueManager.GetQueue(KernelConstants.UpdateChainQueueName).Dispose();
        }
    }
}