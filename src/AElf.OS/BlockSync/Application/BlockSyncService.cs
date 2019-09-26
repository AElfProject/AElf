using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Domain;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockDownloadJobManager _blockDownloadJobManager;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        
        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockFetchService blockFetchService,
            IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService,
            IBlockDownloadJobManager blockDownloadJobManager, IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockFetchService = blockFetchService;
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
            _blockDownloadJobManager = blockDownloadJobManager;
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
                
                EnqueueFetchBlockJob(syncAnnouncementDto);
            }
            else
            {
                await _blockDownloadJobManager.EnqueueAsync(syncAnnouncementDto.SyncBlockHash, syncAnnouncementDto
                .SyncBlockHeight,
                    syncAnnouncementDto.BatchRequestBlockCount, syncAnnouncementDto.SuggestedPeerPubkey);
            }
        }
        
        public async Task SyncByBlockAsync(Chain chain, SyncBlockDto syncBlockDto)
        {
            if (syncBlockDto.BlockWithTransactions.Height <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                EnqueueSyncBlockJob(syncBlockDto.BlockWithTransactions, syncBlockDto.SuggestedPeerPubkey);
            }
            else
            {
                await _blockDownloadJobManager.EnqueueAsync(syncBlockDto.BlockWithTransactions.GetHash(), syncBlockDto.BlockWithTransactions.Height,
                    syncBlockDto.BatchRequestBlockCount, syncBlockDto.SuggestedPeerPubkey);
            }
        }

        private void EnqueueFetchBlockJob(SyncAnnouncementDto syncAnnouncementDto)
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

                if (fetchResult)
                    return;
                if (_announcementCacheProvider.TryGetAnnouncementNextSender(syncAnnouncementDto.SyncBlockHash, out var senderPubKey))
                {
                    syncAnnouncementDto.SuggestedPeerPubkey = senderPubKey;
                    EnqueueFetchBlockJob(syncAnnouncementDto);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private void EnqueueSyncBlockJob(BlockWithTransactions blockWithTransactions, string senderPubkey)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace($"Block sync: sync block, block: {blockWithTransactions}.");
                await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions, senderPubkey);
            }, OSConstants.BlockSyncAttachQueueName);
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