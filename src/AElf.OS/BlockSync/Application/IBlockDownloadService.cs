using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using AElf.Types;
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
        private readonly IBlockSyncAttachService _blockSyncAttachService;

        public ILogger<BlockDownloadService> Logger { get; set; }

        private const int BlockSyncJobLimit = 50;

        public BlockDownloadService(IBlockchainService blockchainService,
            INetworkService networkService,
            IBlockSyncAttachService blockSyncAttachService)
        {
            Logger = NullLogger<BlockDownloadService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockSyncAttachService = blockSyncAttachService;
        }

        public async Task<int> DownloadBlocksAsync(Hash previousBlockHash, long previousBlockHeight,
            int batchRequestBlockCount, string suggestedPeerPubKey)
        {
            Logger.LogDebug(
                $"Trigger download blocks from peers, previous block height: {previousBlockHeight}, previous block hash: {previousBlockHash}");

            var downloadBlockCount = 0;
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

                var blocksWithTransactions = await _networkService.GetBlocksAsync(lastDownloadBlockHash,
                    batchRequestBlockCount, suggestedPeerPubKey);

                if (blocksWithTransactions == null || !blocksWithTransactions.Any())
                {
                    Logger.LogDebug($"No blocks returned, current chain height: {chain.LongestChainHeight}.");
                    break;
                }

                if (blocksWithTransactions.First().Header.PreviousBlockHash != lastDownloadBlockHash)
                {
                    throw new InvalidOperationException(
                        $"Previous block not match previous {lastDownloadBlockHash}, network back {blocksWithTransactions.First().Header.PreviousBlockHash}");
                }

                foreach (var blockWithTransactions in blocksWithTransactions)
                {
                    Logger.LogDebug(
                        $"Processing block {blockWithTransactions},  longest chain hash: {chain.LongestChainHash}, best chain hash : {chain.BestChainHash}");
                    
                    _blockSyncAttachService.EnqueueAttachBlockWithTransactionsJob(blockWithTransactions);

                    downloadBlockCount++;
                }

                var lastBlock = blocksWithTransactions.Last();
                lastDownloadBlockHash = lastBlock.GetHash();
                lastDownloadBlockHeight = lastBlock.Height;
            }

            return downloadBlockCount;
        }
    }
}