using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.OS.Jobs
{
    public class BlockSyncJob
    {
        private const long InitialSyncLimit = 10;
        private const int BlockSyncJobLimit = 1;

        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly NetworkOptions _networkOptions;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<BlockSyncJob> Logger { get; set; }

        public BlockSyncJob(IBlockAttachService blockAttachService,
            IOptionsSnapshot<NetworkOptions> networkOptions,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager)
        {
            Logger = NullLogger<BlockSyncJob>.Instance;
            _networkOptions = networkOptions.Value;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
        }

        public async Task ExecuteAsync(BlockSyncJobArgs args)
        {
            Logger.LogDebug($"Start block sync job, target height: {args.BlockHeight}, target block hash: {args.BlockHash}, peer: {args.SuggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            try
            {
                if (args.BlockHash != null && args.BlockHeight < chain.BestChainHeight + 5)
                {
                    var peerBlockHash = args.BlockHash;
                    var peerBlock = await _blockchainService.GetBlockByHashAsync(peerBlockHash);
                    if (peerBlock != null)
                    {
                        Logger.LogDebug($"Block {peerBlock} already know.");
                        return;
                    }

                    peerBlock = await _networkService.GetBlockByHashAsync(peerBlockHash, args.SuggestedPeerPubKey);
                    if (peerBlock == null)
                    {
                        Logger.LogWarning($"Get null block from peer, request block hash: {peerBlockHash}");
                        return;
                    }

                    _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(peerBlock),
                        KernelConstants.UpdateChainQueueName);
                    return;
                }

                var blockHash = chain.BestChainHash;
                var blockHeight = chain.BestChainHeight;
                Logger.LogDebug(
                    $"Trigger sync blocks from peers, best chain height: {blockHeight}, best chain block hash: {blockHash}");

                var count = _networkOptions.BlockIdRequestCount;
                while (true)
                {
                    // Limit block sync job count, control memory usage
                    chain = await _blockchainService.GetChainAsync();
                    if (chain.LongestChainHeight <= blockHeight - BlockSyncJobLimit)
                    {
                        Logger.LogWarning($"Pause sync task and wait for synced block to be processed, best chain height: {chain.BestChainHeight}");
                        break;
                    }

                    Logger.LogDebug($"Request blocks start with {blockHash}");

                    var blocks = await _networkService.GetBlocksAsync(blockHash, blockHeight, 2, args.SuggestedPeerPubKey);

                    if (blocks == null || !blocks.Any())
                    {
                        if (args.BlockHeight > blockHeight && args.BlockHeight > chain.LongestChainHeight)
                        {
                            Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                            blockHash = chain.LastIrreversibleBlockHash;
                            blockHeight = chain.LastIrreversibleBlockHeight;
                            continue;
                        }

                        Logger.LogDebug($"No blocks returned, current chain height: {chain.LongestChainHeight}.");
                        break;
                    }

                    Logger.LogDebug($"Received [{blocks.First()},...,{blocks.Last()}] ({blocks.Count})");

                    if (blocks.First().Header.PreviousBlockHash != blockHash)
                    {
                        Logger.LogError($"Current job hash : {blockHash}");
                        throw new InvalidOperationException($"Previous block not match previous {blockHash}, network back {blocks.First().Header.PreviousBlockHash}");
                    }

                    foreach (var block in blocks)
                    {
                        if (block == null)
                        {
                            Logger.LogWarning($"Get null block from peer, request block start: {blockHash}");
                            break;
                        }

                        Logger.LogDebug($"Processing block {block},  longest chain hash: {chain.LongestChainHash}, best chain hash : {chain.BestChainHash}");
                        _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                            KernelConstants.UpdateChainQueueName);
                    }

                    var peerBestChainHeight = await _networkService.GetBestChainHeightAsync(args.SuggestedPeerPubKey);
                    if (blocks.Last().Height >= peerBestChainHeight)
                    {
                        break;
                    }

                    var lastBlock = blocks.Last();
                    blockHash = lastBlock.GetHash();
                    blockHeight = lastBlock.Height;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish block sync job");
            }
            finally
            {
                Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
            }
        }
    }
}