using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockDownloadService
    {
        Task<int> DownloadBlocksAsync(Hash previousBlockHash, long previousBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey);
    }

    public class BlockDownloadService : IBlockDownloadService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockDownloadService> Logger { get; set; }

        private const int BlockSyncJobLimit = 10;

        public BlockDownloadService(IBlockAttachService blockAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockDownloadService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task<int> DownloadBlocksAsync(Hash previousBlockHash, long previousBlockHeight,
            int batchRequestBlockCount, string suggestedPeerPubKey)
        {
            Logger.LogDebug(
                $"Trigger download blocks from peers, previous block height: {previousBlockHeight}, previous block hash: {previousBlockHash}");

            var syncBlockCount = 0;
            var lastDownloadBlockHash = previousBlockHash;
            var lastDownloadBlockHeight = previousBlockHeight;
            
            while (true)
            {
                // Limit block sync job count, control memory usage
                var chain = await _blockchainService.GetChainAsync();
                if (chain.LongestChainHeight <= lastDownloadBlockHeight - BlockSyncJobLimit)
                {
                    Logger.LogWarning(
                        $"Pause sync task and wait for synced block to be processed, best chain height: {chain.BestChainHeight}");
                    break;
                }

                Logger.LogDebug($"Request blocks start with {lastDownloadBlockHash}");

                var blocks = await _networkService.GetBlocksAsync(lastDownloadBlockHash, lastDownloadBlockHeight,
                    batchRequestBlockCount, suggestedPeerPubKey);

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
                                _blockSyncStateProvider.BlockSyncJobEnqueueTime = enqueueTimestamp;
                                await _blockAttachService.AttachBlockAsync(block);
                            }
                            finally
                            {
                                _blockSyncStateProvider.BlockSyncJobEnqueueTime = null;
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