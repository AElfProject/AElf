using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Domain;
using AElf.OS.BlockSync.Dto;
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

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockFetchService blockFetchService,
            IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService,
            IBlockDownloadJobManager blockDownloadJobManager)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockFetchService = blockFetchService;
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
            _blockDownloadJobManager = blockDownloadJobManager;
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
                await _blockDownloadJobManager.EnqueueAsync(syncBlockDto.BlockWithTransactions.GetHash(), syncBlockDto.BlockWithTransactions.Height,
                    syncBlockDto.BatchRequestBlockCount, syncBlockDto.SuggestedPeerPubkey);
            }
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