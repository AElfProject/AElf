using System.Collections.Concurrent;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.WebApp.Application.Chain.Infrastructure
{
    public interface ITransactionResultStatusCacheProvider
    {
        void AddTransactionResultStatus(Hash transactionId);
        void ChangeTransactionResultStatus(Hash transactionId, TransactionValidateStatus status);
        TransactionValidateStatus GetTransactionResultStatus(Hash transactionId);
    }

    public class TransactionResultStatusCacheProvider : ITransactionResultStatusCacheProvider
    {
        private readonly WebAppOptions _webAppOptions;

        // TODO: Refactor via System.Runtime.Caching/MemoryCache
        private readonly ConcurrentDictionary<Hash, TransactionValidateStatus> _transactionValidateResults =
            new ConcurrentDictionary<Hash, TransactionValidateStatus>();

        private readonly ConcurrentQueue<TransactionIdWithTimestamp> _transactionIds =
            new ConcurrentQueue<TransactionIdWithTimestamp>();

        public ILogger<TransactionResultStatusCacheProvider> Logger { get; set; }

        public TransactionResultStatusCacheProvider(IOptionsSnapshot<WebAppOptions> webAppOptions)
        {
            _webAppOptions = webAppOptions.Value;

            Logger = NullLogger<TransactionResultStatusCacheProvider>.Instance;
        }

        public void AddTransactionResultStatus(Hash transactionId)
        {
            if (!_transactionValidateResults.TryAdd(transactionId, new TransactionValidateStatus
            {
                TransactionResultStatus = TransactionResultStatus.PendingValidation
            })) return;

            Logger.LogDebug($"Tx {transactionId} entered tx result status cache provider.");
            _transactionIds.Enqueue(new TransactionIdWithTimestamp
            {
                TransactionId = transactionId,
                Timestamp = TimestampHelper.GetUtcNow()
            });
            ClearOldTransactionResultStatus();
        }

        public void ChangeTransactionResultStatus(Hash transactionId, TransactionValidateStatus status)
        {
            if (!_transactionValidateResults.TryGetValue(transactionId, out var currentStatus)) return;

            // Only update status when current status is PendingValidation.
            if (currentStatus.TransactionResultStatus == TransactionResultStatus.PendingValidation)
            {
                Logger.LogDebug($"Tx {transactionId} result status tunes to {status.TransactionResultStatus}.");
                _transactionValidateResults.TryUpdate(transactionId, status, currentStatus);
            }
        }

        private void ClearOldTransactionResultStatus()
        {
            while (_transactionIds.TryPeek(out var firstTransactionIdWithTimestamp) &&
                IsExpired(firstTransactionIdWithTimestamp.Timestamp) &&
                _transactionIds.TryDequeue(out var dequeueTransactionIdWithTimestamp))
            {
                Logger.LogDebug($"Remove tx {dequeueTransactionIdWithTimestamp.TransactionId}");
                _transactionValidateResults.TryRemove(dequeueTransactionIdWithTimestamp.TransactionId, out _);
            }
        }

        private bool IsExpired(Timestamp timestamp)
        {
            return (TimestampHelper.GetUtcNow() - timestamp).Seconds >=
                   _webAppOptions.TransactionResultStatusCacheSeconds;
        }

        public TransactionValidateStatus GetTransactionResultStatus(Hash transactionId)
        {
            _transactionValidateResults.TryGetValue(transactionId, out var status);
            return status;
        }
    }

    public class TransactionValidateStatus
    {
        public TransactionResultStatus TransactionResultStatus { get; set; }
        public string Error { get; set; }
    }

    public class TransactionIdWithTimestamp
    {
        public Hash TransactionId { get; set; }
        public Timestamp Timestamp { get; set; }
    }
}