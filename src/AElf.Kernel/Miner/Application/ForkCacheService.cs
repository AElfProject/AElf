using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public class ForkCacheService : IForkCacheService
    {
        private readonly List<IForkCacheHandler> _forkCacheHandlers;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ForkCacheService(IServiceContainer<IForkCacheHandler> forkCacheHandlers,
            IChainBlockLinkService chainBlockLinkService)
        {
            _chainBlockLinkService = chainBlockLinkService;
            _forkCacheHandlers = forkCacheHandlers.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }

        public async Task MergeAndCleanForkCacheAsync(Hash irreversibleBlockHash, long irreversibleBlockHeight)
        {
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks()
                .Where(b => b.Height <= irreversibleBlockHeight)
                .OrderByDescending(b => b.Height).ToList();

            var irreversibleLink = chainBlockLinks.FirstOrDefault(b => b.BlockHash == irreversibleBlockHash);
            var groupedLinks = chainBlockLinks.GroupBy(b => b.Height).ToDictionary(g => g.Key, g => g.ToList());
            var deletedBlockIndexes = new List<BlockIndex>();
            // delete cache not on best chain
            while (irreversibleLink != null)
            {
                var links = groupedLinks[irreversibleLink.Height].Where(b => b.BlockHash != irreversibleLink.BlockHash)
                    .ToList();
                if (links.Count > 0)
                {
                    foreach (var link in links)
                    {
                        _chainBlockLinkService.RemoveCachedChainBlockLink(link.BlockHash);
                    }

                    var blockHashes = links.Select(b => b.BlockHash).ToArray();
                    chainBlockLinks.RemoveAll(c => c.BlockHash.IsIn(blockHashes));
                    deletedBlockIndexes.AddRange(links.Select(l => new BlockIndex
                    {
                        BlockHash = l.BlockHash,
                        BlockHeight = l.Height
                    }));
                }
                irreversibleLink =
                    chainBlockLinks.FirstOrDefault(b => b.BlockHash == irreversibleLink.PreviousBlockHash);
            }

            await RemoveByBlockHashAsync(deletedBlockIndexes);

            chainBlockLinks.Reverse();

            var blockIndexes = chainBlockLinks.Select(c => new BlockIndex
            {
                BlockHash = c.BlockHash,
                BlockHeight = c.Height
            }).ToList();
            await SetIrreversibleAsync(blockIndexes);

            foreach (var chainBlockLink in chainBlockLinks)
            {
                _chainBlockLinkService.RemoveCachedChainBlockLink(chainBlockLink.BlockHash);
            }
        }
        
        private async Task RemoveByBlockHashAsync(List<BlockIndex> blockIndexes)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                await forkCacheHandler.RemoveForkCacheAsync(blockIndexes);
            }
        }

        private async Task SetIrreversibleAsync(List<BlockIndex> blockIndexes)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                await forkCacheHandler.SetIrreversedCacheAsync(blockIndexes);
            }
        }
    }
}