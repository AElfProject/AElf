using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface IExtraAcceptedTokensCacheProvider
    {
        Dictionary<string, Tuple<int, int>> GetExtraAcceptedTokensInfoFromNormalCache();
        void SetExtraAcceptedTokenInfoToCache(Dictionary<string, Tuple<int, int>> tokenInfos);
        void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, Dictionary<string, Tuple<int, int>> tokenInfos);
        bool TryGetExtraAcceptedTokensInfoFromForkCache(BlockIndex index, out Dictionary<string, Tuple<int, int>> tokenDic);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
        BlockIndex[] GetForkCacheKeys();
    }
    public class ExtraAcceptedTokensCacheProvider : IExtraAcceptedTokensCacheProvider
    {
        private Dictionary<string, Tuple<int, int>> _cache;

        private readonly ConcurrentDictionary<BlockIndex, Dictionary<string, Tuple<int, int>>> _forkCache;

        public ExtraAcceptedTokensCacheProvider()
        {
            _forkCache = new ConcurrentDictionary<BlockIndex, Dictionary<string, Tuple<int, int>>>();
        }
        public Dictionary<string, Tuple<int, int>> GetExtraAcceptedTokensInfoFromNormalCache()
        {
            return _cache;
        }

        public void SetExtraAcceptedTokenInfoToCache(Dictionary<string, Tuple<int, int>> tokenInfos)
        {
            _cache = tokenInfos;
        }

        public void SetExtraAcceptedTokenInfoToForkCache(BlockIndex blockIndex, Dictionary<string, Tuple<int, int>> tokenInfos)
        {
            _forkCache[blockIndex] = tokenInfos;
        }

        public bool TryGetExtraAcceptedTokensInfoFromForkCache(BlockIndex index, out Dictionary<string, Tuple<int, int>> value)
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