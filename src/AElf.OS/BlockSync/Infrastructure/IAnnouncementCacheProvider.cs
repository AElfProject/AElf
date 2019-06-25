using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IAnnouncementCacheProvider
    {
        bool CacheAnnouncement(Hash blockHash, long blockHeight);

        bool ContainsAnnouncement(Hash blockHash, long blockHeight);

        void ClearCache(long blockHeight);
    }

    public class AnnouncementCacheProvider : IAnnouncementCacheProvider, ISingletonDependency
    {
        private SortedDictionary<long, HashSet<Hash>> _cache = new SortedDictionary<long, HashSet<Hash>>();

        public bool CacheAnnouncement(Hash blockHash, long blockHeight)
        {
            if (!_cache.TryGetValue(blockHeight, out var blockHashes))
            {
                _cache.Add(blockHeight, new HashSet<Hash> {blockHash});
                return true;
            }

            return blockHashes.Add(blockHash);
        }

        public bool ContainsAnnouncement(Hash blockHash, long blockHeight)
        {
            if (_cache.TryGetValue(blockHeight, out var blockHashes))
            {
                return blockHashes.Contains(blockHash);
            }

            return false;
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