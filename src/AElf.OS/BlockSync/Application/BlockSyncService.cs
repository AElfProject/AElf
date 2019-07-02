using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncStateProvider blockSyncStateProvider,
            ITaskQueueManager taskQueueManager)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncStateProvider = blockSyncStateProvider;
            _taskQueueManager = taskQueueManager;
        }

        public async Task SyncBlockAsync(Chain chain, SyncBlockDto syncBlockDto)
        {
            if (syncBlockDto.SyncBlockHash != null && syncBlockDto.SyncBlockHeight <= chain.LongestChainHeight + 12)
            {
                EnqueueFetchBlockJob(syncBlockDto, 3);
            }
            else
            {
                EnqueueDownloadBlocksJob(syncBlockDto);
            }
        }
        
        private void EnqueueFetchBlockJob(SyncBlockDto syncBlockDto, int retryTimes)
        {
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    _blockSyncStateProvider.BlockSyncFetchBlockEnqueueTime = enqueueTimestamp;
                    Logger.LogTrace(
                        $"Block sync: Fetch block, block height: {syncBlockDto.SyncBlockHeight}, block hash: {syncBlockDto.SyncBlockHash}, enqueue time: {enqueueTimestamp}");
                    
                    var fetchResult = false;
                    if (BlockAttachAndExecuteQueueIsAvailable())
                    {
                        fetchResult = await _blockFetchService.FetchBlockAsync(syncBlockDto.SyncBlockHash,
                            syncBlockDto.SyncBlockHeight, syncBlockDto.SuggestedPeerPubKey);
                    }

                    if (!fetchResult && retryTimes > 1)
                    {
                        EnqueueFetchBlockJob(syncBlockDto, retryTimes - 1);
                    }
                }
                finally
                {
                    _blockSyncStateProvider.BlockSyncFetchBlockEnqueueTime = null;
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private void EnqueueDownloadBlocksJob(SyncBlockDto syncBlockDto)
        {
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    _blockSyncStateProvider.BlockSyncDownloadBlockEnqueueTime = enqueueTimestamp;
                    Logger.LogTrace(
                        $"Block sync: Download blocks, block height: {syncBlockDto.SyncBlockHeight}, block hash: {syncBlockDto.SyncBlockHash}, enqueue time: {enqueueTimestamp}");

                    if (BlockAttachAndExecuteQueueIsAvailable())
                    {
                        var chain = await _blockchainService.GetChainAsync();
                        
                        if (syncBlockDto.SyncBlockHeight <= chain.LastIrreversibleBlockHeight)
                        {
                            Logger.LogWarning(
                                $"Receive lower header {{ hash: {syncBlockDto.SyncBlockHash}, height: {syncBlockDto.SyncBlockHeight} }}.");
                            return;
                        }

                        var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                            chain.LongestChainHeight, syncBlockDto.BatchRequestBlockCount,
                            syncBlockDto.SuggestedPeerPubKey);

                        if (syncBlockCount == 0)
                        {
                            syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                                chain.BestChainHeight, syncBlockDto.BatchRequestBlockCount,
                                syncBlockDto.SuggestedPeerPubKey);
                        }

                        if (syncBlockCount == 0 && syncBlockDto.SyncBlockHeight > chain.LongestChainHeight + 12)
                        {
                            Logger.LogDebug(
                                $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                            await _blockDownloadService.DownloadBlocksAsync(
                                chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                                syncBlockDto.BatchRequestBlockCount, syncBlockDto.SuggestedPeerPubKey);
                        }
                    }
                }
                finally
                {
                    _blockSyncStateProvider.BlockSyncDownloadBlockEnqueueTime = null;
                }
            }, OSConstants.BlockDownloadQueueName);
        }
        
        private bool BlockAttachAndExecuteQueueIsAvailable()
        {
            var blockSyncAttachBlockEnqueueTime = _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime;
            if (blockSyncAttachBlockEnqueueTime != null && TimestampHelper.GetUtcNow() >
                blockSyncAttachBlockEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncAttachBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync attach queue is too busy, enqueue timestamp: {blockSyncAttachBlockEnqueueTime}");
                return false;
            }

            var blockSyncAttachAndExecuteBlockEnqueueTime =
                _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime;
            if (blockSyncAttachAndExecuteBlockEnqueueTime != null && TimestampHelper.GetUtcNow() >
                blockSyncAttachAndExecuteBlockEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync attach and execute queue is too busy, enqueue timestamp: {blockSyncAttachAndExecuteBlockEnqueueTime}");
                return false;
            }

            return true;
        }
    }
}