using System;
using System.Collections.Generic;
using System.Linq;
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

        public void SetIrreversible(Hash blockHash)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                forkCacheHandler.SetIrreversedCache(blockHash);
            }
        }

        public void CleanCache(Hash irreversibleBlockHash,long irreversibleBlockHeight)
        {
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks()
                .Where(b => b.Height <= irreversibleBlockHeight)
                .OrderByDescending(b => b.Height).ToList();

            var irreversibleLink = chainBlockLinks.FirstOrDefault(b => b.BlockHash == irreversibleBlockHash);
            var groupedLinks = chainBlockLinks.GroupBy(b => b.Height).ToDictionary(g => g.Key, g => g.ToList());
            var deleteBlockHashes = new List<Hash>();
            // delete cache not on best chain
            while (irreversibleLink != null)
            {
                var links = groupedLinks[irreversibleLink.Height].Where(b => b.BlockHash != irreversibleLink.BlockHash).ToList();
                if (links.Count > 0)
                {
                    foreach (var link in links)
                    {
                        _chainBlockLinkService.RemoveCachedChainBlockLink(link.BlockHash);
                    }

                    var blockHashes = links.Select(b => b.BlockHash).ToArray();
                    chainBlockLinks.RemoveAll(c => c.BlockHash.IsIn(blockHashes));
                    deleteBlockHashes.AddRange(blockHashes);
                }

                irreversibleLink = chainBlockLinks.FirstOrDefault(b => b.BlockHash == irreversibleLink.PreviousBlockHash);
            }

            RemoveByBlockHash(deleteBlockHashes);

            chainBlockLinks.Reverse();

            foreach (var chainBlockLink in chainBlockLinks)
            {
                //SetIrreversible(chainBlockLink.BlockHash);
                _chainBlockLinkService.RemoveCachedChainBlockLink(chainBlockLink.BlockHash);
            }
        }
        
        private void RemoveByBlockHash(List<Hash> blockHashes)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                forkCacheHandler.RemoveForkCache(blockHashes);
            }
        }
    }
}