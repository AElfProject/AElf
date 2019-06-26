using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockSyncService> Logger { get; set; }
        
        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey)
        {
            Logger.LogDebug(
                $"Start block sync job, target height: {blockHash}, target block hash: {blockHeight}, peer: {suggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            if (blockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {blockHash}, height: {blockHeight} }} " +
                                $"form {suggestedPeerPubKey}, ignore.");
                return;
            }
            
            if (blockHash != null && blockHeight <= chain.LongestChainHeight + 1)
            {
                await _blockFetchService.FetchBlockAsync(blockHash, blockHeight, suggestedPeerPubKey);
            }
            else
            {
                Logger.LogTrace($"Receive higher header {{ hash: {blockHash}, height: {blockHeight} }} " +
                                $"form {suggestedPeerPubKey}, ignore.");
                var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                    chain.LongestChainHeight, batchRequestBlockCount, suggestedPeerPubKey);

                if (syncBlockCount == 0)
                {
                    syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                        chain.BestChainHeight, batchRequestBlockCount, suggestedPeerPubKey);
                }

                if (syncBlockCount == 0 && blockHeight > chain.LongestChainHeight + 16)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    await _blockDownloadService.DownloadBlocksAsync(chain.LastIrreversibleBlockHash,
                        chain.LastIrreversibleBlockHeight, batchRequestBlockCount, suggestedPeerPubKey);
                }
            }

            Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
        }

        public void SetBlockSyncAnnouncementEnqueueTime(Timestamp timestamp)
        {
            _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = timestamp;
        }
    }
}