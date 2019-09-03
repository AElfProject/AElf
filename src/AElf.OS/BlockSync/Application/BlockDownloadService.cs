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
            var downloadResult = new DownloadBlocksResult();
            var peerPubkey = downloadBlockDto.SuggestedPeerPubkey;

            if (UseSuggestedPeer(downloadBlockDto))
            {
                downloadResult = await DownloadBlocksAsync(downloadBlockDto.PreviousBlockHash,
                    peerPubkey, downloadBlockDto.BatchRequestBlockCount,
                    downloadBlockDto.MaxBlockDownloadCount);
            }
            else
            {
                // If cannot get the blocks, there should be network problems or bad peer,
                // because we have selected peer with lib height greater than or equal to the target height.
                // 1. network problems, need to retry from other peer.
                // 2. not network problems, this peer or the last peer is bad peer, we need to remove it.
                var downloadTargetHeight = downloadBlockDto.PreviousBlockHeight + downloadBlockDto.MaxBlockDownloadCount;
                var exceptedPeers = new List<string> {_blockSyncStateProvider.LastRequestPeerPubkey};
                var retryTimes = 1;

                while (true)
                {
                    peerPubkey = GetRandomPeerPubkey(downloadBlockDto.SuggestedPeerPubkey, downloadTargetHeight,
                        exceptedPeers);

                    downloadResult = await DownloadBlocksAsync(downloadBlockDto.PreviousBlockHash, peerPubkey,
                        downloadBlockDto.BatchRequestBlockCount, downloadBlockDto.MaxBlockDownloadCount);

                    if (downloadResult.Success || retryTimes <= 0) 
                        break;

                    exceptedPeers.Add(peerPubkey);
                    retryTimes--;
                }
                
                if (downloadResult.Success && downloadResult.DownloadBlockCount == 0)
                {
                    Logger.LogWarning("Found bad peer or network problems.");
                    await TryToFindBadPeer(downloadBlockDto.PreviousBlockHash, downloadTargetHeight);
                }
            }

            if (downloadResult.DownloadBlockCount != 0)
            {
                _blockSyncStateProvider.SetDownloadJobTargetState(downloadResult.LastDownloadBlockHash, false);
                _blockSyncStateProvider.LastRequestPeerPubkey = peerPubkey;
            }

            return downloadResult;
        }

        private async Task TryToFindBadPeer(Hash previousBlockHash, long downloadTargetHeight)
        {
            var peers = _networkService.GetPeers().Where(p =>p.LastKnownLibHeight >= downloadTargetHeight);
            var count = 0;
            
            foreach (var peer in peers)
            {
                var result = await DownloadBlocksAsync(previousBlockHash, peer.Info.Pubkey, 1, 1);
                if (result.Success && result.DownloadBlockCount != 0)
                {
                    count++;
                }
            }

            if (count > 2 * peers.Count() / 3)
            {
                // randomPeerPubkey is bad peer
            }
            else if (count < 1 * peers.Count() / 3)
            {
                // _blockSyncStateProvider.LastRequestPeerPubkey is bad peer
            }
        }

        private bool UseSuggestedPeer(DownloadBlockDto downloadBlockDto)
        {
            if (downloadBlockDto.UseSuggestedPeer)
                return true;
            
            var suggestedPeer = _networkService.GetPeerByPubkey(downloadBlockDto.SuggestedPeerPubkey);
            var downloadTargetHeight = downloadBlockDto.PreviousBlockHeight + downloadBlockDto.MaxBlockDownloadCount;
            if (downloadTargetHeight > suggestedPeer.LastKnownLibHeight)
                return true;

            return false;
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

        private async Task<DownloadBlocksResult> DownloadBlocksAsync(Hash previousBlockHash, string peerPubkey, int
            batchRequestBlockCount, int maxBlockDownloadCount)
        {
            var downloadBlockCount = 0;
            var lastDownloadBlockHash = previousBlockHash;
            var lastDownloadBlockHeight = 0L;

            Logger.LogDebug(
                $"Download blocks start with block hash: {lastDownloadBlockHash}, block height: {lastDownloadBlockHeight}, PeerPubkey: {peerPubkey}");

            while (downloadBlockCount < maxBlockDownloadCount)
            {
                var getBlocksResult = await _networkService.GetBlocksAsync(lastDownloadBlockHash,
                    batchRequestBlockCount, peerPubkey);
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