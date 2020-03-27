using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerInvalidDataProvider
    {
        bool TryMarkInvalidData(string host);
        bool TryRemoveInvalidData(string host);
    }

    public class PeerInvalidDataProvider : IPeerInvalidDataProvider, ISingletonDependency
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly ConcurrentDictionary<string, ConcurrentQueue<Timestamp>> _invalidDataCache;

        public PeerInvalidDataProvider()
        {
            _invalidDataCache = new ConcurrentDictionary<string, ConcurrentQueue<Timestamp>>();
        }

        public bool TryMarkInvalidData(string host)
        {
            if (!_invalidDataCache.TryGetValue(host, out var queue))
            {
                queue = new ConcurrentQueue<Timestamp>();
                _invalidDataCache[host] = queue;
            }

            CleanCache(queue);
            if (queue.Count >= NetworkOptions.InvalidDataLimit)
                return false;

            queue.Enqueue(TimestampHelper.GetUtcNow());

            return true;
        }

        public bool TryRemoveInvalidData(string host)
        {
            return _invalidDataCache.TryRemove(host, out _);
        }

        private void CleanCache(ConcurrentQueue<Timestamp> queue)
        {
            while (!queue.IsEmpty
                   && queue.TryPeek(out var timestamp)
                   && timestamp.AddMilliseconds(NetworkOptions.InvalidDataTimeout) < TimestampHelper.GetUtcNow())
            {
                queue.TryDequeue(out _);
            }
        }
    }
}