using System.Collections.Concurrent;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class AnnouncementCacheProvider : IAnnouncementCacheProvider, ISingletonDependency
    {
        private ConcurrentDictionary<Hash, long> _cache = new ConcurrentDictionary<Hash, long>();

        private ConcurrentQueue<Hash> _toBeCleanedKeys = new ConcurrentQueue<Hash>();

        public bool TryAddAnnouncementCache(Hash blockHash, long blockHeight)
        {
            if (_cache.ContainsKey(blockHash))
            {
                return false;
            }

            _toBeCleanedKeys.Enqueue(blockHash);
            while (_toBeCleanedKeys.Count > 100)
            {
                if (_toBeCleanedKeys.TryDequeue(out var cleanKey))
                    _cache.TryRemove(cleanKey, out _);
            }

            _cache[blockHash] = blockHeight;

            return true;
        }
    }
}