using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface IMultiChainBlockInfoCache
    {
        void AddBlockInfoCache(int chainId, BlockInfoCache blockInfoCache);
        BlockInfoCache GetBlockInfoCache(int chainId);
    }
    
    public class MultiChainBlockInfoCache : IMultiChainBlockInfoCache, ISingletonDependency
    {
        private readonly Dictionary<int, BlockInfoCache> _blockInfoCaches = new Dictionary<int, BlockInfoCache>();
        public void AddBlockInfoCache(int chainId, BlockInfoCache blockInfoCache)
        {
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