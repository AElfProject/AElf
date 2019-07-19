using System.Collections.Concurrent;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class AnnouncementCacheProvider : IAnnouncementCacheProvider, ISingletonDependency
    {
        private ConcurrentDictionary<Hash, AnnouncementCache> _cache = new ConcurrentDictionary<Hash, AnnouncementCache>();

        private ConcurrentQueue<Hash> _toBeCleanedKeys = new ConcurrentQueue<Hash>();

        public bool TryAddAnnouncementCache(Hash blockHash, long blockHeight, string senderPubKey)
        {
            //TODO: block height should be checked in case of malicious attacks.
            if (_cache.TryGetValue(blockHash, out var announcementCache))
            {
                var res = announcementCache.SenderPubKeys.IsEmpty;
                announcementCache.SenderPubKeys.Enqueue(senderPubKey);
                return res;
            }

            _toBeCleanedKeys.Enqueue(blockHash);
            while (_toBeCleanedKeys.Count > 100)
            {
                if (_toBeCleanedKeys.TryDequeue(out var cleanKey))
                    _cache.TryRemove(cleanKey, out _);
            }

            _cache.AddOrUpdate(blockHash, new AnnouncementCache
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                SenderPubKeys = new ConcurrentQueue<string>(new[] {senderPubKey})
            }, (hash, cache) =>
            {
                cache.SenderPubKeys.Enqueue(senderPubKey);
                return cache;
            });

            return true;
        }

        public bool TryGetAnnouncementNextSender(Hash blockHash, out string senderPubKey)
        {
            if (_cache.TryGetValue(blockHash, out var announcementCache))
            {
                return announcementCache.SenderPubKeys.TryDequeue(out senderPubKey);
            }

            senderPubKey = null;
            return false;
        }
    }

    class AnnouncementCache
    {
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }
        public ConcurrentQueue<string> SenderPubKeys { get; set; }
    }
}