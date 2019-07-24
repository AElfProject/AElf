using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncJobService : IBlockSyncJobService
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockchainService _blockchainService;

        public Logger<BlockSyncJobService> Logger { get; set; } 

        public BlockSyncJobService(IBlockFetchService blockFetchService, IBlockDownloadService blockDownloadService, 
            IBlockchainService blockchainService)
        {
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockchainService = blockchainService;
        }

        public async Task<bool> DoFetchBlockAsync(BlockFetchJobDto blockFetchJobDto, Func<IEnumerable<string>, bool> isQueueAvailable)
        {
            Logger.LogTrace(
                $"Block sync: Fetch block, block height: {blockFetchJobDto.BlockHeight}, block hash: {blockFetchJobDto.BlockHash}.");
            
            if (!isQueueAvailable(new[] {OSConstants.BlockSyncAttachQueueName, KernelConstants.UpdateChainQueueName})) 
                return false;
            
            var fetchResult = await _blockFetchService.FetchBlockAsync(blockFetchJobDto.BlockHash,
                blockFetchJobDto.BlockHeight, blockFetchJobDto.SuggestedPeerPubkey);

            return fetchResult;
        }

        public async Task<bool> DoDownloadBlocksAsync(BlockDownloadJobDto blockDownloadJobDto, Func<IEnumerable<string>, bool> isQueueAvailable)
        {
            Logger.LogTrace(
                $"Block sync: Download blocks, block height: {blockDownloadJobDto.BlockHeight}, block hash: {blockDownloadJobDto.BlockHash}.");

            if (!isQueueAvailable(new[] {OSConstants.BlockSyncAttachQueueName, KernelConstants.UpdateChainQueueName}))
                return false;
            
            var chain = await _blockchainService.GetChainAsync();

            if (blockDownloadJobDto.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockDownloadJobDto.BlockHash}, height: {blockDownloadJobDto.BlockHeight} }}.");
                return false;
            }

            var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                chain.LongestChainHeight, blockDownloadJobDto.BatchRequestBlockCount, blockDownloadJobDto.SuggestedPeerPubkey);

            if (syncBlockCount == 0)
            {
                syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                    chain.BestChainHeight, blockDownloadJobDto.BatchRequestBlockCount, blockDownloadJobDto.SuggestedPeerPubkey);
            }

            if (syncBlockCount == 0 && blockDownloadJobDto.BlockHeight >
                chain.LongestChainHeight + BlockSyncConstants.BlockSyncModeHeightOffset)
            {
                Logger.LogDebug(
                    $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(
                    chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                    blockDownloadJobDto.BatchRequestBlockCount, blockDownloadJobDto.SuggestedPeerPubkey);
            }

            return syncBlockCount > 0;
        }
    }
}