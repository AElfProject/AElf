using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerInvalidTransactionProvider
    {
        bool TryMarkInvalidTransaction(string host, Hash transactionId);
        bool TryRemoveInvalidRecord(string host);
    }

    public class PeerInvalidTransactionProvider : IPeerInvalidTransactionProvider, ISingletonDependency
    {
        private readonly NetworkOptions _networkOptions;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<InvalidTransaction>> _invalidTransactionCache;
        private readonly ConcurrentDictionary<string, int> _hostInvalidTransactionIdCache;

        public ILogger<PeerInvalidTransactionProvider> Logger { get; set; }

        public PeerInvalidTransactionProvider(IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _networkOptions = networkOptions.Value;
            _invalidTransactionCache = new ConcurrentDictionary<string, ConcurrentQueue<InvalidTransaction>>();
            _hostInvalidTransactionIdCache = new ConcurrentDictionary<string, int>();
            Logger = NullLogger<PeerInvalidTransactionProvider>.Instance;
        }

        public bool TryMarkInvalidTransaction(string host, Hash transactionId)
        {
            var txId = transactionId.ToHex();
            if (_hostInvalidTransactionIdCache.TryAdd(GetHostInvalidTransactionIdCacheKey(host, txId), 1))
            {
                if (!_invalidTransactionCache.TryGetValue(host, out var queue))
                {
                    queue = new ConcurrentQueue<InvalidTransaction>();
                    _invalidTransactionCache[host] = queue;
                }

                CleanCache(host, queue);

                queue.Enqueue(new InvalidTransaction
                {
                    TransactionId = txId,
                    Timestamp = TimestampHelper.GetUtcNow()
                });

                Logger.LogDebug($"Mark peer invalid transaction. host: {host}, count: {queue.Count}");

                return queue.Count <= _networkOptions.PeerInvalidTransactionLimit;
            }

            return true;
        }

        public bool TryRemoveInvalidRecord(string host)
        {
            if (_invalidTransactionCache.TryGetValue(host, out var invalidTransactions))
            {
                foreach (var invalidTransaction in invalidTransactions)
                {
                    _hostInvalidTransactionIdCache.TryRemove(
                        GetHostInvalidTransactionIdCacheKey(host, invalidTransaction.TransactionId), out _);
                }

                return _invalidTransactionCache.TryRemove(host, out _);
            }

            return false;
        }

        private void CleanCache(string host, ConcurrentQueue<InvalidTransaction> queue)
        {
            while (!queue.IsEmpty
                   && queue.TryPeek(out var invalidTransaction)
                   && invalidTransaction.Timestamp.AddMilliseconds(_networkOptions.PeerInvalidTransactionTimeout) <
                   TimestampHelper.GetUtcNow())
            {
                _hostInvalidTransactionIdCache.TryRemove(
                    GetHostInvalidTransactionIdCacheKey(host, invalidTransaction.TransactionId), out _);
                queue.TryDequeue(out _);
            }
        }

        private string GetHostInvalidTransactionIdCacheKey(string host, string transactionId)
        {
            return host + transactionId;
        }
    }

    public class InvalidTransaction
    {
        public string TransactionId { get; set; }

        public Timestamp Timestamp { get; set; }
    }
}