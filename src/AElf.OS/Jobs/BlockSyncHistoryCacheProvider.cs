using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Jobs
{
    public interface IBlockSyncHistoryCacheProvider
    {
        bool AddCache(Hash blockHash, long blockHeight);

        void ClearCache(long blockHeight);
    }

    public class BlockSyncHistoryCacheProvider : IBlockSyncHistoryCacheProvider, ISingletonDependency
    {
        private HashSet<Hash> _cache = new HashSet<Hash>();
        
        private SortedDictionary<long,List<Hash>> _sortedCache = new SortedDictionary<long, List<Hash>>();

        public bool AddCache(Hash blockHash, long blockHeight)
        {
            while (_cache.Count > 10000)
            {
                var toRemoveCache = _sortedCache.First();
                
                foreach (var toRemoveHash in toRemoveCache.Value)
                {
                    _cache.Remove(toRemoveHash);
                }
                _sortedCache.Remove(toRemoveCache.Key);
            }

            if (_cache.Add(blockHash))
            {
                if (!_sortedCache.TryGetValue(blockHeight,out var blockHashes))
                {
                    _sortedCache.Add(blockHeight, new List<Hash> {blockHash});
                }
                else
                {
                    blockHashes.Add(blockHash);
                }

                return true;
            }

            return false;
        }

        public void ClearCache(long blockHeight)
        {
            while (true)
            {
                var firstCache = _sortedCache.FirstOrDefault();

                if (firstCache.Key == 0 || firstCache.Key > blockHeight)
                {
                    break;
                }

                foreach (var blockHash in firstCache.Value)
                {
                    _cache.Remove(blockHash);
                }

                _sortedCache.Remove(firstCache.Key);
            }
        }
    }
}