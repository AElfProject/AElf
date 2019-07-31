using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using Microsoft.Extensions.Logging;

namespace AElf.OS.BlockSync.Application
{
    public class AnnouncementSyncService : IAnnouncementSyncService
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;

        public ILogger<AnnouncementSyncService> Logger { get; set; }

        public AnnouncementSyncService(IBlockSyncAttachService blockSyncAttachService, IBlockFetchService blockFetchService, IBlockSyncQueueService blockSyncQueueService)
        {
            _blockSyncAttachService = blockSyncAttachService;
            _blockFetchService = blockFetchService;
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
                await _blockDownloadJobManager.EnqueueAsync(syncAnnouncementDto.SyncBlockHash, syncAnnouncementDto
                        .SyncBlockHeight,
                    syncAnnouncementDto.BatchRequestBlockCount, syncAnnouncementDto.SuggestedPeerPubkey);
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
                        syncAnnouncementDto.SyncBlockHeight, syncAnnouncementDto.SuggestedPeerPubkey);
                }

                if (!fetchResult && retryTimes > 1)
                {
                    EnqueueFetchBlockJob(syncAnnouncementDto, retryTimes - 1);
                }
            }, OSConstants.BlockFetchQueueName);
        }
    }
}