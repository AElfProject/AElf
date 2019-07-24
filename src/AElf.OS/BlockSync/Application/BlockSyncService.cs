using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockSyncJobService _blockSyncJobService;

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService, IBlockSyncValidationService blockSyncValidationService, IBlockSyncJobService blockSyncJobService)
        {
            Logger = NullLogger<BlockSyncService>.Instance;
            
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
            _blockSyncValidationService = blockSyncValidationService;
            _blockSyncJobService = blockSyncJobService;
        }

        public async Task SyncByBlockAsync(Chain chain, SyncBlockDto syncBlockDto)
        {
            if (!await _blockSyncValidationService.ValidateBlockAsync(chain, syncBlockDto.BlockWithTransactions, syncBlockDto.SuggestedPeerPubkey))
            {
                return;
            }

            Logger.LogDebug(
                $"Start full block sync job, block: {syncBlockDto.BlockWithTransactions}, peer: {syncBlockDto.SuggestedPeerPubkey}.");


            if (syncBlockDto.BlockWithTransactions.Height <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                EnqueueSyncBlockJob(syncBlockDto.BlockWithTransactions);
            }
            else
            {
                SyncMultiBlocks(syncBlockDto.BlockWithTransactions.GetHash(), syncBlockDto.BlockWithTransactions.Height,
                    syncBlockDto.SuggestedPeerPubkey, syncBlockDto.BatchRequestBlockCount);
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