using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IMultiChainBlockInfoCacheProvider
    {
        void AddBlockInfoCache(int chainId, BlockInfoCache blockInfoCache);
        BlockInfoCache GetBlockInfoCache(int chainId);
        int Size { get; }
    }
    
    public class MultiChainBlockInfoCacheProvider : IMultiChainBlockInfoCacheProvider, ISingletonDependency
    {
        private readonly Dictionary<int, BlockInfoCache> _blockInfoCaches = new Dictionary<int, BlockInfoCache>();
        public int Size => _blockInfoCaches.Count;
        public void AddBlockInfoCache(int chainId, BlockInfoCache blockInfoCache)
        {
            if (blockInfoCache == null)
                return;
            if(!_blockInfoCaches.TryGetValue(chainId, out _))
                _blockInfoCaches.Add(chainId, blockInfoCache);
            _blockInfoCaches[chainId] = blockInfoCache;
        }

        public BlockInfoCache GetBlockInfoCache(int chainId)
        {
            return !_blockInfoCaches.TryGetValue(chainId, out _) ? null : _blockInfoCaches[chainId];
        }
    }
}