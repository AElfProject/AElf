using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.OS.Jobs
{
    public class BlockSyncJob
    {
        private const int BlockSyncJobLimit = 10;
        
        private readonly TimeSpan _blockSyncJobAgeLimit = TimeSpan.FromSeconds(0.5);

        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly NetworkOptions _networkOptions;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly INetworkSyncStateProvider _networkSyncStateProvider;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;

        public ILogger<BlockSyncJob> Logger { get; set; }

        public BlockSyncJob(IBlockAttachService blockAttachService,
            IOptionsSnapshot<NetworkOptions> networkOptions,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager,
            INetworkSyncStateProvider networkSyncStateProvider,
            IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncJob>.Instance;
            _networkOptions = networkOptions.Value;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _networkSyncStateProvider = networkSyncStateProvider;
            _announcementCacheProvider = announcementCacheProvider;
        }

        public async Task ExecuteAsync(BlockSyncJobArgs args)
        {
            Logger.LogDebug($"Start block sync job, target height: {args.BlockHeight}, target block hash: {args.BlockHash}, peer: {args.SuggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            if (args.BlockHash != null && args.BlockHeight < chain.BestChainHeight + 5)
            {
                await FetchBlockAsync(args.BlockHash, args.BlockHeight, args.SuggestedPeerPubKey);
            }
            else
            {
                _announcementCacheProvider.ClearCache(chain.LastIrreversibleBlockHeight);
                if (!_announcementCacheProvider.CacheAnnouncement(args.BlockHash, args.BlockHeight))
                {
                    Logger.LogWarning($"The block have been synchronized, block hash: {args.BlockHash}");
                    return;
                }
                
                if (_networkSyncStateProvider.BlockSyncJobEnqueueTime != null
                    && DateTime.UtcNow - _networkSyncStateProvider.BlockSyncJobEnqueueTime.ToDateTime() >
                    _blockSyncJobAgeLimit)
                {
                    Logger.LogWarning($"Queue is too busy, block sync job enqueue timestamp: {_networkSyncStateProvider.BlockSyncJobEnqueueTime.ToDateTime()}");
                    return;
                }
                
                var syncFromBestChainResult = await SyncBlocksAsync(chain.BestChainHash, chain.BestChainHeight,
                    args.BlockHash, args.BlockHeight, args.SuggestedPeerPubKey);

                if (syncFromBestChainResult == 0 && args.BlockHeight > chain.LongestChainHeight)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    await SyncBlocksAsync(chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                        args.BlockHash, args.BlockHeight, args.SuggestedPeerPubKey);
                }
            }

            Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
        }

        private async Task FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey)
        {
            var peerBlock = await _blockchainService.GetBlockByHashAsync(blockHash);
            if (peerBlock != null)
            {
                Logger.LogDebug($"Block {peerBlock} already know.");
                return;
            }

            peerBlock = await _networkService.GetBlockByHashAsync(blockHash, suggestedPeerPubKey);
            if (peerBlock == null)
            {
                Logger.LogWarning($"Get null block from peer, request block hash: {blockHash}");
                return;
            }

            _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(peerBlock),
                KernelConstants.UpdateChainQueueName);
        }

        private async Task<int> SyncBlocksAsync(Hash previousBlockHash, long previousBlockHeight, Hash targetBlockHash, long
            targetBlockHeight, string suggestedPeerPubKey)
        {
            Logger.LogDebug($"Trigger sync blocks from peers, previous block height: {previousBlockHeight}, previous block hash: {previousBlockHash}");

            var syncBlockCount = 0;
            var lastDownloadBlockHash = previousBlockHash;
            var lastDownloadBlockHeight = previousBlockHeight;
            while (true)
            {
                // Limit block sync job count, control memory usage
                var chain = await _blockchainService.GetChainAsync();
                if (chain.LongestChainHeight <= lastDownloadBlockHeight - BlockSyncJobLimit)
                {
                    Logger.LogWarning($"Pause sync task and wait for synced block to be processed, best chain height: {chain.BestChainHeight}");
                    break;
                }

                Logger.LogDebug($"Request blocks start with {lastDownloadBlockHash}");

                var blocks = await _networkService.GetBlocksAsync(lastDownloadBlockHash, lastDownloadBlockHeight, _networkOptions.BlockIdRequestCount, suggestedPeerPubKey);

                if (blocks == null || !blocks.Any())
                {
                    Logger.LogDebug($"No blocks returned, current chain height: {chain.LongestChainHeight}.");
                    break;
                }

                if (blocks.First().Header.PreviousBlockHash != lastDownloadBlockHash)
                {
                    Logger.LogError($"Current job hash : {lastDownloadBlockHash}");
                    throw new InvalidOperationException(
                        $"Previous block not match previous {lastDownloadBlockHash}, network back {blocks.First().Header.PreviousBlockHash}");
                }

                foreach (var block in blocks)
                {
                    Logger.LogDebug(
                        $"Processing block {block},  longest chain hash: {chain.LongestChainHash}, best chain hash : {chain.BestChainHash}");
                    
                    var enqueueTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
                    _taskQueueManager.Enqueue(async () =>
                        {
                            try
                            {
                                _networkSyncStateProvider.BlockSyncJobEnqueueTime = enqueueTimestamp;
                                await _blockAttachService.AttachBlockAsync(block);
                            }
                            finally
                            {
                                _networkSyncStateProvider.BlockSyncJobEnqueueTime = null;
                            }
                        },
                        KernelConstants.UpdateChainQueueName);

                    syncBlockCount++;
                }

                var lastBlock = blocks.Last();
                lastDownloadBlockHash = lastBlock.GetHash();
                lastDownloadBlockHeight = lastBlock.Height;
            }

            return syncBlockCount;
        }
    }
}