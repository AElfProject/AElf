using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IMultiChainBlockInfoCacheProvider
    {
        void AddBlockInfoCache(int remoteChainId, BlockInfoCache blockInfoCache);
        BlockInfoCache GetBlockInfoCache(int remoteChainId);

        bool ContainsChain(int remoteChainId);
        int Size { get; }
        IEnumerable<int> CachedChainIds { get; }
    }
    
    public class MultiChainBlockInfoCacheProvider : IMultiChainBlockInfoCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<int, BlockInfoCache> _blockInfoCaches = new ConcurrentDictionary<int, BlockInfoCache>();
        public bool ContainsChain(int remoteChainId)
        {
            return _blockInfoCaches.ContainsKey(remoteChainId);
        }

        public int Size => _blockInfoCaches.Count;
        public IEnumerable<int> CachedChainIds => _blockInfoCaches.Keys.ToList();

        public void AddBlockInfoCache(int remoteChainId, BlockInfoCache blockInfoCache)
        {
            if (blockInfoCache == null)
                return;
            _blockInfoCaches.TryAdd(remoteChainId, blockInfoCache);
        }

        public BlockInfoCache GetBlockInfoCache(int remoteChainId)
        {
            return !_blockInfoCaches.TryGetValue(remoteChainId, out _) ? null : _blockInfoCaches[remoteChainId];
        }
    }
}