using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network.Grpc
{
    public class BoundedExpirationCache
    {
        private readonly int _timeout;
        private readonly int _maximumCapacity;

        private readonly ConcurrentQueue<QueuedHash> _expiryHashQueue;
        private readonly ConcurrentDictionary<Hash, Timestamp> _hashLookup;

        public BoundedExpirationCache(int maximumCapacity, int timeout)
        {
            _timeout = timeout;
            _maximumCapacity = maximumCapacity;
            
            _expiryHashQueue = new ConcurrentQueue<QueuedHash>();
            _hashLookup = new ConcurrentDictionary<Hash, Timestamp>();
        }

        public bool HasHash(Hash hash, bool clean = true)
        {
            if (clean)
                CleanExpired();

            return _hashLookup.ContainsKey(hash);
        }

        public bool TryAdd(Hash itemHash)
        {
            CleanExpired();

            // we've reached the maximum buffered items.
            if (_expiryHashQueue.Count >= _maximumCapacity)
                return false;

            var now = TimestampHelper.GetUtcNow();

            // check for existence.
            if (!_hashLookup.TryAdd(itemHash, now)) 
                return false;
            
            _expiryHashQueue.Enqueue(new QueuedHash
            {
                ItemHash = itemHash, EnqueueTime = now
            });

            return true;
        }

        private void CleanExpired()
        {
            // clean old items.
            while (!_expiryHashQueue.IsEmpty && _expiryHashQueue.TryPeek(out var queuedHash)
                                             && queuedHash.EnqueueTime.AddMilliseconds(_timeout) < TimestampHelper.GetUtcNow())
            {
                if (_expiryHashQueue.TryDequeue(out var expiredItem))
                    _hashLookup.TryRemove(expiredItem.ItemHash, out _);
            }
        }
    }
}