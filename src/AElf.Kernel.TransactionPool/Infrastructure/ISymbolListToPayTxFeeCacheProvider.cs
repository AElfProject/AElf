using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ISymbolListToPayTxFeeCacheProvider
    {
        List<AvailableTokenInfoInCache> GetExtraAcceptedTokensInfoFromNormalCache();
        void SetExtraAcceptedTokenInfoToCache(List<AvailableTokenInfoInCache> tokenInfos);
        void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, List<AvailableTokenInfoInCache> tokenInfos);
        bool TryGetExtraAcceptedTokensInfoFromForkCache(BlockIndex index, out List<AvailableTokenInfoInCache> tokenInfos);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
        BlockIndex[] GetForkCacheKeys();
    }
    public class SymbolListToPayTxFeeCacheProvider : ISymbolListToPayTxFeeCacheProvider, ISingletonDependency
    {
        private List<AvailableTokenInfoInCache> _cache;

        private readonly ConcurrentDictionary<BlockIndex, List<AvailableTokenInfoInCache>> _forkCache;

        public SymbolListToPayTxFeeCacheProvider()
        {
            _forkCache = new ConcurrentDictionary<BlockIndex, List<AvailableTokenInfoInCache>>();
        }
        public List<AvailableTokenInfoInCache> GetExtraAcceptedTokensInfoFromNormalCache()
        {
            return _cache;
        }

        public void SetExtraAcceptedTokenInfoToCache(List<AvailableTokenInfoInCache> tokenInfos)
        {
            _cache = tokenInfos;
        }

        public void SetExtraAcceptedTokenInfoToForkCache(BlockIndex blockIndex, List<AvailableTokenInfoInCache> tokenInfos)
        {
            _forkCache[blockIndex] = tokenInfos;
        }

        public bool TryGetExtraAcceptedTokensInfoFromForkCache(BlockIndex index, out List<AvailableTokenInfoInCache> value)
        {
            return _forkCache.TryGetValue(index, out value);
        }

        public void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes.Where(blockIndex => _forkCache.TryGetValue(blockIndex, out _)))
            {
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SyncCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if (!_forkCache.TryGetValue(blockIndex, out var extraTokenInfo)) continue;
                _cache = extraTokenInfo;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public BlockIndex[] GetForkCacheKeys()
        {
            return  _forkCache.Keys.ToArray();
        }
    }
}