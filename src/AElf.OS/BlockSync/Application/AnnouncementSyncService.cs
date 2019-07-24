using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.OS.BlockSync.Application
{
    public class AnnouncementSyncService : IAnnouncementSyncService
    {
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockSyncJobService _blockSyncJobService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        
        public AnnouncementSyncService(IBlockSyncValidationService blockSyncValidationService, 
            IBlockSyncJobService blockSyncJobService, 
            IBlockSyncQueueService blockSyncQueueService, 
            IAnnouncementCacheProvider announcementCacheProvider)
        {
            _blockSyncValidationService = blockSyncValidationService;
            _blockSyncJobService = blockSyncJobService;
            _blockSyncQueueService = blockSyncQueueService;
            _announcementCacheProvider = announcementCacheProvider;
        }

        public ILogger<AnnouncementSyncService> Logger { get; set; }
        
        public async Task SyncByAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubkey,
            int networkOptionsBlockIdRequestCount)
        {
            if (!await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement, senderPubkey))
            {
                return;
            }

            Logger.LogDebug(
                $"Start block sync job, target height: {blockAnnouncement.BlockHeight}, target block hash: {blockAnnouncement.BlockHash}, peer: {senderPubkey}");

            if (blockAnnouncement.BlockHash != null && blockAnnouncement.BlockHeight <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                SyncOneBlock(blockAnnouncement.BlockHash, blockAnnouncement.BlockHeight, senderPubkey);
            }
            else
            {
                SyncMultiBlocks(blockAnnouncement.BlockHash, blockAnnouncement.BlockHeight, senderPubkey,
                    networkOptionsBlockIdRequestCount);
            }
        }

        public void SyncOneBlock(Hash blockHash, long blockHeight, string suggestedPeerPubkey, int retryTimes = 0)
        {
            if (!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockFetchQueueName))
            {
                Logger.LogWarning("Block sync fetch queue is too busy.");
                return;
            }

            _blockSyncQueueService.Enqueue(async () =>
            {
                var result = await _blockSyncJobService.DoFetchBlockAsync(
                    new BlockFetchJobDto
                        {BlockHash = blockHash, BlockHeight = blockHeight, SuggestedPeerPubkey = suggestedPeerPubkey},
                    _blockSyncValidationService.ValidateQueueAvailability);
                if (result)
                    return;
                if (++retryTimes < BlockSyncConstants.FetchBlockRetryTimes)
                {
                    SyncOneBlock(blockHash, blockHeight, suggestedPeerPubkey, retryTimes);
                }
                else if (_announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out var senderPubKey))
                {
                    SyncOneBlock(blockHash, blockHeight, senderPubKey);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        public void SyncMultiBlocks(Hash blockHash, long blockHeight, string suggestedPeerPubkey, int batchRequestCount)
        {
            if (!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockDownloadQueueName))
            {
                Logger.LogWarning("Block sync download queue is too busy.");
                return;
            }

            _blockSyncQueueService.Enqueue(
                async () =>
                {
                    await _blockSyncJobService.DoDownloadBlocksAsync(new BlockDownloadJobDto
                    {
                        BlockHash = blockHash, BlockHeight = blockHeight, BatchRequestBlockCount = batchRequestCount,
                        SuggestedPeerPubkey = suggestedPeerPubkey
                    }, _blockSyncValidationService.ValidateQueueAvailability);
                }, OSConstants.BlockDownloadQueueName);
        }
    }
}