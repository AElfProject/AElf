using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IBlockchainStateMergingService
    {
        Task MergeBlockStateAsync(long lastIrreversibleBlockHeight, Hash lastIrreversibleBlockHash);
    }

    public class BlockchainStateMergingService : IBlockchainStateMergingService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        public ILogger<BlockchainStateMergingService> Logger { get; set; }

        public BlockchainStateMergingService(IBlockchainService blockchainService, IBlockchainStateManager blockchainStateManager)
        {
            _blockchainService = blockchainService;
            _blockchainStateManager = blockchainStateManager;
            Logger = NullLogger<BlockchainStateMergingService>.Instance;
        }

        public async Task MergeBlockStateAsync(long lastIrreversibleBlockHeight, Hash lastIrreversibleBlockHash)
        {
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            var firstHeightToMerge = chainStateInfo.BlockHeight == 0L ? KernelConstants.GenesisBlockHeight : chainStateInfo.BlockHeight + 1;
            var mergeCount = lastIrreversibleBlockHeight - firstHeightToMerge;
            if (mergeCount < 0)
            {
                Logger.LogWarning($"Last merge height: {chainStateInfo.BlockHeight}, lib height: {lastIrreversibleBlockHeight}, needn't merge");
                return;
            }

            var hashes = await _blockchainService.GetReversedBlockHashes(lastIrreversibleBlockHash, (int) mergeCount) ?? new List<KeyValuePair<Hash, long>>();

            if (chainStateInfo.Status != ChainStateMergingStatus.Common)
                hashes.Add(new KeyValuePair<Hash, long>(chainStateInfo.MergingBlockHash, -1));

            hashes.Reverse();

            hashes.Add(new KeyValuePair<Hash, long>(lastIrreversibleBlockHash, lastIrreversibleBlockHeight));

            Logger.LogTrace($"Merge lib height: {lastIrreversibleBlockHeight}, lib block hash: {lastIrreversibleBlockHash}, merge count: {hashes.Count}");

            foreach (var (hash, height) in hashes.Select(x => (x.Key, x.Value)))
            {
                try
                {
                    Logger.LogDebug($"Merging state {chainStateInfo} for block {hash} at height {height}");
                    await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, hash);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Exception while merge state {chainStateInfo} for block {hash} at height {height}");
                    break;
                }
            }
        }
    }
}