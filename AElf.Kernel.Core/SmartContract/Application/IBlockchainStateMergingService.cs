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

    public class BlockchainStateMergingService:IBlockchainStateMergingService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        public ILogger<BlockchainStateMergingService> Logger { get; set; }

        public BlockchainStateMergingService(IBlockchainService blockchainService,
            IBlockchainStateManager blockchainStateManager)
        {
            _blockchainService = blockchainService;
            _blockchainStateManager = blockchainStateManager;
            Logger = NullLogger<BlockchainStateMergingService>.Instance;
        }

        public async Task MergeBlockStateAsync(long lastIrreversibleBlockHeight, Hash lastIrreversibleBlockHash)
        {
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            Logger.LogInformation(chainStateInfo.ToString());
            var firstHeightToMerge = chainStateInfo.BlockHeight == 0L
                ? ChainConsts.GenesisBlockHeight
                : chainStateInfo.BlockHeight + 1;
            var count = lastIrreversibleBlockHeight - firstHeightToMerge;

            var hashes = await _blockchainService.GetReversedBlockHashes(lastIrreversibleBlockHash, (int) count) ??
                         new List<Hash>();

            hashes.Reverse();

            hashes.Add(lastIrreversibleBlockHash);

            foreach (var (hash, height) in hashes.Select((x, i) => (x, i + firstHeightToMerge)))
            {
                try
                {
                    Logger.LogInformation($"Merging state for block {hash} at height: {height}");
                    await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, hash);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                }
            }
        }
    }
}