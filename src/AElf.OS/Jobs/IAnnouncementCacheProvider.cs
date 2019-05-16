using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Jobs
{
    public interface IAnnouncementCacheProvider
    {
        bool AddCache(Hash blockHash, long blockHeight);

        void ClearCache(long blockHeight);
    }

    public class AnnouncementCacheProvider : IAnnouncementCacheProvider, ISingletonDependency
    {
        private SortedDictionary<long, HashSet<Hash>> _cache = new SortedDictionary<long, HashSet<Hash>>();

        public bool AddCache(Hash blockHash, long blockHeight)
        {
            if (!_cache.TryGetValue(blockHeight, out var blockHashes))
            {
                _cache.Add(blockHeight, new HashSet<Hash> {blockHash});
                return true;
            }

            return blockHashes.Add(blockHash);
        }

        public void ClearCache(long blockHeight)
        {
            while (_cache.Count > 0)
            {
                var firstCache = _cache.First();

                if (firstCache.Key > blockHeight)
                {
                    break;
                }

                _cache.Remove(firstCache.Key);
            }
        }
    }
}