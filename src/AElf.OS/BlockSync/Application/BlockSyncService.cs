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
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncQueueService blockSyncQueueService, IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncQueueService = blockSyncQueueService;
            _announcementCacheProvider = announcementCacheProvider;
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

                if (fetchResult)
                    return;
                if (retryTimes > 1)
                {
                    EnqueueFetchBlockJob(syncAnnouncementDto, retryTimes - 1);
                }
                else if (_announcementCacheProvider.TryGetAnnouncementNextSender(syncAnnouncementDto.SyncBlockHash, out var senderPubKey))
                {
                    Logger.LogTrace(
                        $"Try get announcement next sender for block height {syncAnnouncementDto.SyncBlockHeight}, block hash: {syncAnnouncementDto.SyncBlockHash}, sender pub key: {senderPubKey}.");
                    EnqueueFetchBlockJob(new SyncAnnouncementDto
                    {
                        SyncBlockHash = syncAnnouncementDto.SyncBlockHash,
                        SyncBlockHeight = syncAnnouncementDto.SyncBlockHeight,
                        SuggestedPeerPubKey = senderPubKey,
                        BatchRequestBlockCount = syncAnnouncementDto.BatchRequestBlockCount
                    }, BlockSyncConstants.FetchBlockRetryTimes);
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