using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerInvalidTransactionProvider
    {
        bool TryMarkInvalidTransaction(string host);
        bool TryRemoveInvalidRecord(string host);
    }

    public class PeerInvalidTransactionProvider : IPeerInvalidTransactionProvider, ISingletonDependency
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly ConcurrentDictionary<string, ConcurrentQueue<Timestamp>> _invalidTransactionCache;

        public PeerInvalidTransactionProvider()
        {
            _invalidTransactionCache = new ConcurrentDictionary<string, ConcurrentQueue<Timestamp>>();
        }

        public bool TryMarkInvalidTransaction(string host)
        {
            if (!_invalidTransactionCache.TryGetValue(host, out var queue))
            {
                queue = new ConcurrentQueue<Timestamp>();
                _invalidTransactionCache[host] = queue;
            }

            CleanCache(queue);
            if (queue.Count >= NetworkOptions.PeerInvalidTransactionLimit)
                return false;

            queue.Enqueue(TimestampHelper.GetUtcNow());

            return true;
        }

        public bool TryRemoveInvalidRecord(string host)
        {
            return _invalidTransactionCache.TryRemove(host, out _);
        }

        private void CleanCache(ConcurrentQueue<Timestamp> queue)
        {
            while (!queue.IsEmpty
                   && queue.TryPeek(out var timestamp)
                   && timestamp.AddMilliseconds(NetworkOptions.PeerInvalidTransactionTimeout) < TimestampHelper.GetUtcNow())
            {
                queue.TryDequeue(out _);
            }
        }
    }
}