using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        
        public BlockSyncValidationServiceTests()
        {
            _blockSyncValidationService = GetRequiredService<IBlockSyncValidationService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
        }

        [Fact]
        public async Task ValidateBeforeEnqueue_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight + 1;

            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ValidateBeforeEnqueue_WithEnqueueTime_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight + 1;

            _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-3);
            _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-1);
            _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime =
                TimestampHelper.GetUtcNow().AddMilliseconds(-200);

            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBeforeEnqueue_BlockSyncQueueIsBusy()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight + 1;

            _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-5);

            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateBeforeEnqueue_BlockSyncAttachQueueIsBusy()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight + 1;

            _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-4);

            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateBeforeEnqueue_BlockSyncAttachAndExecuteQueueIsBusy()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight + 1;

            _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime = TimestampHelper.GetUtcNow().AddMilliseconds(-800);

            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateBeforeEnqueue_AlreadySynchronized()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight - 1;

            _announcementCacheProvider.AddAnnouncementCache(syncBlockHash, syncBlockHeight);
            
            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateBeforeEnqueue_LessThenLIBHeight()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var syncBlockHash = Hash.FromString("SyncBlockHash");
            var syncBlockHeight = chain.LastIrreversibleBlockHeight - 1;
            
            var validateResult =
                await _blockSyncValidationService.ValidateBeforeEnqueue(syncBlockHash, syncBlockHeight);
            
            validateResult.ShouldBeFalse();
        }
    }
}