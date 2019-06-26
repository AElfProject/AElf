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

        public async Task SyncBlockAsync(SyncBlockDto syncBlockDto)
        {
            Logger.LogDebug(
                $"Start block sync job, target height: {syncBlockDto.SyncBlockHash}, target block hash: {syncBlockDto.SyncBlockHeight}, peer: {syncBlockDto.SuggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            if (syncBlockDto.SyncBlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace(
                    $"Receive lower header {{ hash: {syncBlockDto.SyncBlockHash}, height: {syncBlockDto.SyncBlockHeight} }} form {syncBlockDto.SuggestedPeerPubKey}, ignore.");
                return;
            }

            var syncResult = false;
            if (QueueIsAvailable())
            {
                if (syncBlockDto.SyncBlockHash != null && syncBlockDto.SyncBlockHeight <= chain.LongestChainHeight + 1)
                {
                    syncResult = await _blockFetchService.FetchBlockAsync(syncBlockDto.SyncBlockHash,
                        syncBlockDto.SyncBlockHeight, syncBlockDto.SuggestedPeerPubKey);
                }
                else
                {
                    Logger.LogTrace(
                        $"Receive higher header {{ hash: {syncBlockDto.SyncBlockHash}, height: {syncBlockDto.SyncBlockHeight} }} form {syncBlockDto.SuggestedPeerPubKey}, ignore.");
                    var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                        chain.LongestChainHeight, syncBlockDto.BatchRequestBlockCount,
                        syncBlockDto.SuggestedPeerPubKey);

                    if (syncBlockCount == 0)
                    {
                        syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                            chain.BestChainHeight, syncBlockDto.BatchRequestBlockCount,
                            syncBlockDto.SuggestedPeerPubKey);
                    }
                    
                    syncResult = syncBlockCount > 0;

                    if (syncBlockCount == 0 && syncBlockDto.SyncBlockHeight > chain.LongestChainHeight + 16)
                    {
                        Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                        syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(
                            chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                            syncBlockDto.BatchRequestBlockCount, syncBlockDto.SuggestedPeerPubKey);
                        syncResult = syncBlockCount > 0;
                    }
                }
            }

            if (!syncResult && syncBlockDto.SyncRetryTimes > 1)
            {
                syncBlockDto.SyncRetryTimes -= 1;
                EnqueueSyncBlockJob(syncBlockDto);
            }

            Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
        }

        public void EnqueueSyncBlockJob(SyncBlockDto syncBlockDto)
        {
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = enqueueTimestamp;
                    await SyncBlockAsync(syncBlockDto);
                }
                finally
                {
                    _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = null;
                }
            }, OSConsts.BlockSyncQueueName);
        }

        private bool QueueIsAvailable()
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