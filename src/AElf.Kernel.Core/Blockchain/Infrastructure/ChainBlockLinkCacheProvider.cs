using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IChainBlockLinkCacheProvider
    {
        ChainBlockLink GetChainBlockLink(Hash blockHash);
        List<ChainBlockLink> GetChainBlockLinks();
        void SetChainBlockLink(ChainBlockLink chainBlockLink);

        void RemoveChainBlockLink(Hash blockHash);
    }

    public class ChainBlockLinkCacheProvider : IChainBlockLinkCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, ChainBlockLink> _cachedChainBlockLinks =
            new ConcurrentDictionary<Hash, ChainBlockLink>();

        public ChainBlockLink GetChainBlockLink(Hash blockHash)
        {
            _cachedChainBlockLinks.TryGetValue(blockHash, out var chainBlockLink);
            return chainBlockLink;
        }

        public List<ChainBlockLink> GetChainBlockLinks()
        {
            return _cachedChainBlockLinks.Values.ToList();
        }

        public void SetChainBlockLink(ChainBlockLink chainBlockLink)
        {
            _cachedChainBlockLinks[chainBlockLink.BlockHash] = chainBlockLink;
        }

        public void RemoveChainBlockLink(Hash blockHash)
        {
            _cachedChainBlockLinks.TryRemove(blockHash, out _);
        }
    }
}