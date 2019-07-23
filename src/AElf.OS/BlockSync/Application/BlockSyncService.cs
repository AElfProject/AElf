using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService, IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncAttachService = blockSyncAttachService;
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

                EnqueueDownloadBlocksJob(syncAnnouncementDto.SyncBlockHash, syncAnnouncementDto.SyncBlockHeight,
                    syncAnnouncementDto.BatchRequestBlockCount, syncAnnouncementDto.SuggestedPeerPubkey);
            }
        }
        
        public async Task SyncByBlockAsync(Chain chain, SyncBlockDto syncBlockDto)
        {
            if (syncBlockDto.BlockWithTransactions.Height <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                EnqueueSyncBlockJob(syncBlockDto.BlockWithTransactions);
            }
            else
            {
                if(!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockDownloadQueueName))
                {
                    Logger.LogWarning($"Block sync download queue is too busy. block: {syncBlockDto.BlockWithTransactions}");
                    return;
                }

                EnqueueDownloadBlocksJob(syncBlockDto.BlockWithTransactions.GetHash(), syncBlockDto.BlockWithTransactions.Height,
                    syncBlockDto.BatchRequestBlockCount, syncBlockDto.SuggestedPeerPubkey);
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

                if (fetchResult)
                    return;
                if (retryTimes > 1)
                {
                    EnqueueFetchBlockJob(syncAnnouncementDto, retryTimes - 1);
                }
                else if (_announcementCacheProvider.TryGetAnnouncementNextSender(syncAnnouncementDto.SyncBlockHash, out var senderPubkey))
                {
                    Logger.LogTrace(
                        $"Try get announcement next sender for block height {syncAnnouncementDto.SyncBlockHeight}, block hash: {syncAnnouncementDto.SyncBlockHash}, sender pub key: {senderPubkey}.");
                    EnqueueFetchBlockJob(new SyncAnnouncementDto
                    {
                        SyncBlockHash = syncAnnouncementDto.SyncBlockHash,
                        SyncBlockHeight = syncAnnouncementDto.SyncBlockHeight,
                        SuggestedPeerPubkey = senderPubkey,
                        BatchRequestBlockCount = syncAnnouncementDto.BatchRequestBlockCount
                    }, BlockSyncConstants.FetchBlockRetryTimes);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private void EnqueueDownloadBlocksJob(Hash syncBlockHash, long syncBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubkey)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: Download blocks, block height: {syncBlockHeight}, block hash: {syncBlockHash}.");

                if (ValidateQueueAvailability())
                {
                    var chain = await _blockchainService.GetChainAsync();

                    if (syncBlockHeight <= chain.LastIrreversibleBlockHeight)
                    {
                        Logger.LogWarning(
                            $"Receive lower header {{ hash: {syncBlockHash}, height: {syncBlockHeight} }}.");
                        return;
                    }

                    var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                        chain.LongestChainHeight, batchRequestBlockCount, suggestedPeerPubkey);

                    if (syncBlockCount == 0)
                    {
                        syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                            chain.BestChainHeight, batchRequestBlockCount, suggestedPeerPubkey);
                    }

                    if (syncBlockCount == 0 && syncBlockHeight >
                        chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
                    {
                        Logger.LogDebug(
                            $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                        await _blockDownloadService.DownloadBlocksAsync(
                            chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                            batchRequestBlockCount, suggestedPeerPubkey);
                    }
                }
            }, OSConstants.BlockDownloadQueueName);
        }

        private void EnqueueSyncBlockJob(BlockWithTransactions blockWithTransactions)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace($"Block sync: sync block, block: {blockWithTransactions}.");
                await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions);
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