using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly ICachedBlockProvider _cachedBlockProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IForkCacheService _forkCacheService;

        public BlockAcceptedEventHandler(ICachedBlockProvider cachedBlockProvider,
            IBlockchainService blockchainService,
            IForkCacheService forkCacheService
        )
        {
            _cachedBlockProvider = cachedBlockProvider;
            _blockchainService = blockchainService;
            _forkCacheService = forkCacheService;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            var chain = await _blockchainService.GetChainAsync();

            var reversibleBlocks = _cachedBlockProvider.GetBlocks()
                .Where(b => b.Height <= chain.LastIrreversibleBlockHeight && !b.IsIrreversibleBlock)
                .OrderByDescending(b => b.Height).ToList();

            var blockCache = reversibleBlocks.FirstOrDefault(b => b.BlockHash == chain.LastIrreversibleBlockHash);
            var groupedBlocks = reversibleBlocks.GroupBy(b => b.Height).ToDictionary(g => g.Key, g => g.ToList());
            var deleteBlockHashes = new List<Hash>();
            // delete cache not on best chain
            while (blockCache != null)
            {
                var blocks = groupedBlocks[blockCache.Height].Where(b => b.BlockHash != blockCache.BlockHash).ToList();
                if (blocks.Count > 0)
                {
                    foreach (var reversibleBlock in blocks)
                    {
                        _cachedBlockProvider.RemoveBlock(reversibleBlock.BlockHash);
                    }

                    var blockHashes = blocks.Select(b => b.BlockHash).ToArray();
                    reversibleBlocks.RemoveAll(rb => rb.BlockHash.IsIn(blockHashes));
                    deleteBlockHashes.AddRange(blockHashes);
                }

                blockCache = reversibleBlocks.FirstOrDefault(b => b.BlockHash == blockCache.PreviousBlockHash);
            }

            _forkCacheService.RemoveByBlockHash(deleteBlockHashes);

            reversibleBlocks.Reverse();

            foreach (var reversibleBlock in reversibleBlocks)
            {
                _cachedBlockProvider.SetLastIrreversible(reversibleBlock.BlockHash);
            }

            // delete block cache under lib height - 512
            var height = Math.Max(chain.LastIrreversibleBlockHeight - 512, 0);
            _cachedBlockProvider.RemoveBlockUnderHeight(height);
        }
    }
}