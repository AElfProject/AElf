using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.OS.Network.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadService : IBlockDownloadService
    {
        private readonly INetworkService _networkService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockDownloadService> Logger { get; set; }

        public BlockDownloadService(INetworkService networkService,
            IBlockSyncAttachService blockSyncAttachService,
            IBlockSyncQueueService blockSyncQueueService,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockDownloadService>.Instance;

            _networkService = networkService;
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task<DownloadBlocksResult> DownloadBlocksAsync(DownloadBlockDto downloadBlockDto)
        {
            var downloadBlockCount = 0;
            var lastDownloadBlockHash = downloadBlockDto.PreviousBlockHash;
            var lastDownloadBlockHeight = downloadBlockDto.PreviousBlockHeight;

            while (downloadBlockCount < downloadBlockDto.MaxBlockDownloadCount)
            {
                Logger.LogDebug(
                    $"Request blocks start with block hash: {lastDownloadBlockHash}, block height: {lastDownloadBlockHeight}");

                var blocksWithTransactions = await _networkService.GetBlocksAsync(lastDownloadBlockHash,
                    downloadBlockDto.BatchRequestBlockCount, downloadBlockDto.SuggestedPeerPubkey);

                if (blocksWithTransactions == null || !blocksWithTransactions.Any())
                {
                    Logger.LogWarning("No blocks returned.");
                    break;
                }

                if (blocksWithTransactions.First().Header.PreviousBlockHash != lastDownloadBlockHash)
                {
                    throw new InvalidOperationException(
                        $"Previous block not match previous {lastDownloadBlockHash}, network back {blocksWithTransactions.First().Header.PreviousBlockHash}");
                }

                foreach (var blockWithTransactions in blocksWithTransactions)
                {
                    Logger.LogDebug($"Processing block {blockWithTransactions}.");

                    _blockSyncQueueService.Enqueue(
                        async () =>
                        {
                            await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions,
                                async () =>
                                {
                                    _blockSyncStateProvider.TryUpdateDownloadJobTargetState(blockWithTransactions.GetHash(),true);
                                });
                        },
                        OSConstants.BlockSyncAttachQueueName);

                    downloadBlockCount++;
                }

                var lastBlock = blocksWithTransactions.Last();
                lastDownloadBlockHash = lastBlock.GetHash();
                lastDownloadBlockHeight = lastBlock.Height;
            }

            if (lastDownloadBlockHash != null)
                _blockSyncStateProvider.SetDownloadJobTargetState(lastDownloadBlockHash, false);

            return new DownloadBlocksResult
            {
                DownloadBlockCount = downloadBlockCount,
                LastDownloadBlockHash = lastDownloadBlockHash,
                LastDownloadBlockHeight = lastDownloadBlockHeight
            };
        }

        public bool ValidateQueueAvailabilityBeforeDownload()
        {
            if (!_blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockSyncAttachQueueName))
            {
                Logger.LogWarning("Block sync attach queue is too busy.");
                return false;
            }

            if (!_blockSyncQueueService.ValidateQueueAvailability(KernelConstants.UpdateChainQueueName))
            {
                Logger.LogWarning("Block sync attach and execute queue is too busy.");
                return false;
            }

            return true;
        }

        public void RemoveDownloadJobTargetState(Hash targetBlockHash)
        {
            if (targetBlockHash != null)
                _blockSyncStateProvider.TryRemoveDownloadJobTargetState(targetBlockHash);
        }

        public bool IsNotReachedDownloadTarget(Hash targetBlockHash)
        {
            return _blockSyncStateProvider.TryGetDownloadJobTargetState(targetBlockHash, out var state)
                   && state == false;
        }
    }
}