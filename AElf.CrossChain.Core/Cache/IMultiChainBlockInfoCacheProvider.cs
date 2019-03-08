using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IMultiChainBlockInfoCacheProvider
    {
        void AddBlockInfoCache(int remoteChainId, BlockInfoCache blockInfoCache);
        BlockInfoCache GetBlockInfoCache(int remoteChainId);

        bool ContainsChain(int remoteChainId);
        int Size { get; }
    }
    
    public class MultiChainBlockInfoCacheProvider : IMultiChainBlockInfoCacheProvider, ISingletonDependency
    {
        private readonly Dictionary<int, BlockInfoCache> _blockInfoCaches = new Dictionary<int, BlockInfoCache>();
        public bool ContainsChain(int remoteChainId)
        {
            return _blockInfoCaches.ContainsKey(remoteChainId);
        }

        public int Size => _blockInfoCaches.Count;
        public void AddBlockInfoCache(int remoteChainId, BlockInfoCache blockInfoCache)
        {
            if (blockInfoCache == null)
                return;
            if(!_blockInfoCaches.TryGetValue(remoteChainId, out _))
                _blockInfoCaches.Add(remoteChainId, blockInfoCache);
            _blockInfoCaches[remoteChainId] = blockInfoCache;
        }

        public BlockInfoCache GetBlockInfoCache(int remoteChainId)
        {
            return !_blockInfoCaches.TryGetValue(remoteChainId, out _) ? null : _blockInfoCaches[remoteChainId];
        }
    }
}