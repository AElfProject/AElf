using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task SyncByAnnouncementAsync(Chain chain, SyncAnnouncementDto syncAnnouncementDto)
        {
            if (syncAnnouncementDto.SyncBlockHash != null && syncAnnouncementDto.SyncBlockHeight <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                if(!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockFetchQueueName))
                {
                    Logger.LogWarning("Block sync fetch queue is too busy.");
                    return;
                }
                
                EnqueueFetchBlockJob(syncAnnouncementDto, BlockSyncConstants.FetchBlockRetryTimes);
            }
            else
            {
                if(!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockDownloadQueueName))
                {
                    Logger.LogWarning("Block sync download queue is too busy.");
                    return;
                }
                
                EnqueueDownloadBlocksJob(syncAnnouncementDto);
            }
        }

        private void EnqueueFetchBlockJob(SyncAnnouncementDto syncAnnouncementDto, int retryTimes)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: Fetch block, block height: {syncAnnouncementDto.SyncBlockHeight}, block hash: {syncAnnouncementDto.SyncBlockHash}.");

                var fetchResult = false;
                if (ValidateQueueAvailability())
                {
                    fetchResult = await _blockFetchService.FetchBlockAsync(syncAnnouncementDto.SyncBlockHash,
                        syncAnnouncementDto.SyncBlockHeight, syncAnnouncementDto.SuggestedPeerPubKey);
                }

                if (!fetchResult && retryTimes > 1)
                {
                    EnqueueFetchBlockJob(syncAnnouncementDto, retryTimes - 1);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private void EnqueueDownloadBlocksJob(SyncAnnouncementDto syncAnnouncementDto)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: Download blocks, block height: {syncAnnouncementDto.SyncBlockHeight}, block hash: {syncAnnouncementDto.SyncBlockHash}.");

                if (ValidateQueueAvailability())
                {
                    var chain = await _blockchainService.GetChainAsync();

                    if (syncAnnouncementDto.SyncBlockHeight <= chain.LastIrreversibleBlockHeight)
                    {
                        Logger.LogWarning(
                            $"Receive lower header {{ hash: {syncAnnouncementDto.SyncBlockHash}, height: {syncAnnouncementDto.SyncBlockHeight} }}.");
                        return;
                    }

                    var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                        chain.LongestChainHeight, syncAnnouncementDto.BatchRequestBlockCount,
                        syncAnnouncementDto.SuggestedPeerPubKey);

                    if (syncBlockCount == 0)
                    {
                        syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                            chain.BestChainHeight, syncAnnouncementDto.BatchRequestBlockCount,
                            syncAnnouncementDto.SuggestedPeerPubKey);
                    }

                    if (syncBlockCount == 0 && syncAnnouncementDto.SyncBlockHeight >
                        chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
                    {
                        Logger.LogDebug(
                            $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                        await _blockDownloadService.DownloadBlocksAsync(
                            chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                            syncAnnouncementDto.BatchRequestBlockCount, syncAnnouncementDto.SuggestedPeerPubKey);
                    }
                }
            }, OSConstants.BlockDownloadQueueName);
        }

        private bool ValidateQueueAvailability()
        {
            if(!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockSyncAttachQueueName))
            {
                Logger.LogWarning("Block sync attach queue is too busy.");
                return false;
            }

            if(!_blockSyncQueueService.ValidateQueueAvailability(KernelConstants.UpdateChainQueueName))
            {
                Logger.LogWarning("Block sync attach and execute queue is too busy.");
                return false;
            }

            return true;
        }
    }
}