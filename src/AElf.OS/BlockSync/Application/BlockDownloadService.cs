using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
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

        /// <summary>
        /// Download and attach blocks
        /// UseSuggestedPeer == true: Download blocks from suggested peer directly;
        /// Target download height > peer lib height, download blocks from suggested peer;
        /// Target download height <= peer lib height, select a random peer to download.
        /// </summary>
        /// <param name="downloadBlockDto"></param>
        /// <returns></returns>
        public async Task<DownloadBlocksResult> DownloadBlocksAsync(DownloadBlockDto downloadBlockDto)
        {
            if (downloadBlockDto.UseSuggestedPeer)
            {
                return await DownloadBlocksAsync(downloadBlockDto, downloadBlockDto.SuggestedPeerPubkey);
            }

            var suggestedPeer = _networkService.GetPeerByPubkey(downloadBlockDto.SuggestedPeerPubkey);
            var downloadTargetHeight = downloadBlockDto.PreviousBlockHeight + downloadBlockDto.MaxBlockDownloadCount;
            if (downloadTargetHeight > suggestedPeer.LastKnownLibHeight)
            {
                return await DownloadBlocksAsync(downloadBlockDto, downloadBlockDto.SuggestedPeerPubkey);
            }

            var randomPeerPubkey = GetRandomPeerPubkey(downloadBlockDto.SuggestedPeerPubkey, downloadTargetHeight,
                new List<string> {_blockSyncStateProvider.LastRequestPeerPubkey});

            var downloadResult = await DownloadBlocksAsync(downloadBlockDto, randomPeerPubkey);
            if (downloadResult.DownloadBlockCount == 0)
            {
                // TODO: Handle bad peer or network problems.
                // If cannot get the blocks, there should be network problems or bad peer,
                // because we have selected peer with lib height greater than or equal to the target height.
                // 1. network problems, need to retry from other peer.
                // 2. not network problems, this peer or the last peer is bad peer, we need to remove it.
                //
                // But now we have no way to know if it is a network problem through the network service,
                // so we need to modify the implementation of NetworkService.GetBlocksAsync.
                Logger.LogWarning("Found bad peer or network problems.");

                // Network problem
                if (true)
                {
                    var nextRandomPeerPubkey = GetRandomPeerPubkey(downloadBlockDto.SuggestedPeerPubkey,
                        downloadTargetHeight, new List<string>
                        {
                            _blockSyncStateProvider.LastRequestPeerPubkey,
                            randomPeerPubkey
                        });
                    downloadResult = await DownloadBlocksAsync(downloadBlockDto, nextRandomPeerPubkey);

                    if (downloadResult.DownloadBlockCount == 0)
                    {
                        
                    }
                }
                // Bad peer
                else
                {
                    var peers = _networkService.GetPeers().Where(p => p.LastKnownLibHeight >= downloadTargetHeight);
                    foreach (var peer in peers)
                    {
                        
                    }
                }
            }

            return downloadResult;
        }

        private string GetRandomPeerPubkey(string defaultPeerPubkey, long peerLibHeight, List<string> exceptedPeers)
        {
            var random = new Random();
            var peers = _networkService.GetPeers()
                .Where(p => p.LastKnownLibHeight >= peerLibHeight &&
                            (exceptedPeers.IsNullOrEmpty() || !exceptedPeers.Contains(p.Info.Pubkey)))
                .ToList();

            var randomPeerPubkey = peers.Count == 0
                ? defaultPeerPubkey
                : peers[random.Next() % peers.Count].Info.Pubkey;

            return randomPeerPubkey;
        }

        private async Task<DownloadBlocksResult> DownloadBlocksAsync(DownloadBlockDto downloadBlockDto,
            string peerPubkey)
        {
            var downloadBlockCount = 0;
            var lastDownloadBlockHash = downloadBlockDto.PreviousBlockHash;
            var lastDownloadBlockHeight = downloadBlockDto.PreviousBlockHeight;

            Logger.LogDebug(
                $"Download blocks start with block hash: {lastDownloadBlockHash}, block height: {lastDownloadBlockHeight}, PeerPubkey: {peerPubkey}");

            while (downloadBlockCount < downloadBlockDto.MaxBlockDownloadCount)
            {
                var getBlocksResult = await _networkService.GetBlocksAsync(lastDownloadBlockHash,
                    downloadBlockDto.BatchRequestBlockCount, peerPubkey);
                if (!getBlocksResult.Success)
                {
                    return new DownloadBlocksResult
                    {
                        Success = false
                    };
                }

                var blocksWithTransactions = getBlocksResult.Payload;
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
                    EnqueueAttachBlockJob(blockWithTransactions, peerPubkey);
                    downloadBlockCount++;
                }

                var lastBlock = blocksWithTransactions.Last();
                lastDownloadBlockHash = lastBlock.GetHash();
                lastDownloadBlockHeight = lastBlock.Height;
            }

            if (downloadBlockCount != 0)
            {
                _blockSyncStateProvider.SetDownloadJobTargetState(lastDownloadBlockHash, false);
                _blockSyncStateProvider.LastRequestPeerPubkey = peerPubkey;
            }

            return new DownloadBlocksResult
            {
                Success = true,
                DownloadBlockCount = downloadBlockCount,
                LastDownloadBlockHash = lastDownloadBlockHash,
                LastDownloadBlockHeight = lastDownloadBlockHeight
            };
        }

        private void EnqueueAttachBlockJob(BlockWithTransactions blockWithTransactions, string senderPubkey)
        {
            _blockSyncQueueService.Enqueue(
                async () =>
                {
                    await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions, senderPubkey,
                        () =>
                        {
                            _blockSyncStateProvider.TryUpdateDownloadJobTargetState(
                                blockWithTransactions.GetHash(), true);
                            return Task.CompletedTask;
                        });
                },
                OSConstants.BlockSyncAttachQueueName);
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