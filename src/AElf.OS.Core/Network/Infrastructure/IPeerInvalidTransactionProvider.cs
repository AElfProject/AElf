using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly NetworkOptions _networkOptions;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<Timestamp>> _invalidTransactionCache;
        
        public ILogger<PeerInvalidTransactionProvider> Logger { get; set; }

        public PeerInvalidTransactionProvider(IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _networkOptions = networkOptions.Value;
            _invalidTransactionCache = new ConcurrentDictionary<string, ConcurrentQueue<Timestamp>>();
            
            Logger = NullLogger<PeerInvalidTransactionProvider>.Instance;
        }

        public bool TryMarkInvalidTransaction(string host)
        {
            if (!_invalidTransactionCache.TryGetValue(host, out var queue))
            {
                queue = new ConcurrentQueue<Timestamp>();
                _invalidTransactionCache[host] = queue;
            }

            CleanCache(queue);

            Logger.LogDebug($"Mark peer invalid transaction. host: {host}, count: {queue.Count}");
            
            if (queue.Count >= _networkOptions.PeerInvalidTransactionLimit)
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
                   && timestamp.AddMilliseconds(_networkOptions.PeerInvalidTransactionTimeout) < TimestampHelper.GetUtcNow())
            {
                queue.TryDequeue(out _);
            }
        }
    }
}