using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
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

        public ILogger<BlockSyncService> Logger { get; set; }

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task SyncByAnnounceAsync(Chain chain, SyncAnnounceDto syncAnnounceDto)
        {
            if (syncAnnounceDto.SyncBlockHash != null && syncAnnounceDto.SyncBlockHeight <=
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                EnqueueFetchBlockJob(syncAnnounceDto, BlockSyncConstants.FetchBlockRetryTimes);
            }
            else
            {
                EnqueueDownloadBlocksJob(syncAnnounceDto);
            }
        }
        
        public async Task SyncByBlockAsync(BlockWithTransactions blockWithTransactions)
        {
            EnqueueSyncBlockJob(blockWithTransactions, BlockSyncConstants.FetchBlockRetryTimes);
        }
        
        private void EnqueueFetchBlockJob(SyncAnnounceDto syncAnnounceDto, int retryTimes)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: Fetch block, block height: {syncAnnounceDto.SyncBlockHeight}, block hash: {syncAnnounceDto.SyncBlockHash}.");

                var fetchResult = false;
                if (BlockAttachAndExecuteQueueIsAvailable())
                {
                    fetchResult = await _blockFetchService.FetchBlockAsync(syncAnnounceDto.SyncBlockHash,
                        syncAnnounceDto.SyncBlockHeight, syncAnnounceDto.SuggestedPeerPubKey);
                }

                if (!fetchResult && retryTimes > 1)
                {
                    EnqueueFetchBlockJob(syncAnnounceDto, retryTimes - 1);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private void EnqueueDownloadBlocksJob(SyncAnnounceDto syncAnnounceDto)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: Download blocks, block height: {syncAnnounceDto.SyncBlockHeight}, block hash: {syncAnnounceDto.SyncBlockHash}.");

                if (BlockAttachAndExecuteQueueIsAvailable())
                {
                    var chain = await _blockchainService.GetChainAsync();

                    if (syncAnnounceDto.SyncBlockHeight <= chain.LastIrreversibleBlockHeight)
                    {
                        Logger.LogWarning(
                            $"Receive lower header {{ hash: {syncAnnounceDto.SyncBlockHash}, height: {syncAnnounceDto.SyncBlockHeight} }}.");
                        return;
                    }

                    var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                        chain.LongestChainHeight, syncAnnounceDto.BatchRequestBlockCount,
                        syncAnnounceDto.SuggestedPeerPubKey);

                    if (syncBlockCount == 0)
                    {
                        syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                            chain.BestChainHeight, syncAnnounceDto.BatchRequestBlockCount,
                            syncAnnounceDto.SuggestedPeerPubKey);
                    }

                    if (syncBlockCount == 0 && syncAnnounceDto.SyncBlockHeight >
                        chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
                    {
                        Logger.LogDebug(
                            $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                        await _blockDownloadService.DownloadBlocksAsync(
                            chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                            syncAnnounceDto.BatchRequestBlockCount, syncAnnounceDto.SuggestedPeerPubKey);
                    }
                }
            }, OSConstants.BlockDownloadQueueName);
        }

        private void EnqueueSyncBlockJob(BlockWithTransactions blockWithTransactions, int retryTimes)
        {
            _blockSyncQueueService.Enqueue(async () =>
            {
                Logger.LogTrace(
                    $"Block sync: sync block, block height: {blockWithTransactions.Height}, block hash: {blockWithTransactions.GetHash()}.");

                if (BlockAttachAndExecuteQueueIsAvailable())
                {
                    _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions);
                }
                else if (retryTimes > 1)
                {
                    EnqueueSyncBlockJob(blockWithTransactions, retryTimes - 1);
                }
            }, OSConstants.BlockFetchQueueName);
        }

        private bool BlockAttachAndExecuteQueueIsAvailable()
        {
            if(!_blockSyncQueueService.IsQueueAvailable(OSConstants.BlockSyncAttachQueueName))
            {
                Logger.LogWarning($"Block sync attach queue is too busy.");
                return false;
            }

            if(!_blockSyncQueueService.IsQueueAvailable(KernelConstants.UpdateChainQueueName))
            {
                Logger.LogWarning(
                    $"Block sync attach and execute queue is too busy.");
                return false;
            }

            return true;
        }
    }
}