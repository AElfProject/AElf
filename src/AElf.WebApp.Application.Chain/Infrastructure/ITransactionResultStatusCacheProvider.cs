using System.Collections.Concurrent;
using AElf.Types;
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

        private readonly ConcurrentDictionary<Hash, TransactionValidateStatus> _transactionValidateResults =
            new ConcurrentDictionary<Hash, TransactionValidateStatus>();

        private readonly ConcurrentQueue<Hash> _transactionIds = new ConcurrentQueue<Hash>();

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

            Logger.LogTrace($"Tx {transactionId} entered tx result status cache provider.");
            _transactionIds.Enqueue(transactionId);
            ClearOldTransactionResultStatus();
        }

        public void ChangeTransactionResultStatus(Hash transactionId, TransactionValidateStatus status)
        {
            if (!_transactionValidateResults.TryGetValue(transactionId, out var currentStatus)) return;

            // Only update status when current status is PendingValidation.
            if (currentStatus.TransactionResultStatus == TransactionResultStatus.PendingValidation)
            {
                Logger.LogTrace($"Tx {transactionId} result status tunes to {status}.");
                _transactionValidateResults.TryUpdate(transactionId, status, currentStatus);
            }
        }

        private void ClearOldTransactionResultStatus()
        {
            if (_transactionIds.Count < _webAppOptions.TransactionResultStatusCacheSize) return;

            if (_transactionIds.TryDequeue(out var firstTransactionId))
            {
                _transactionValidateResults.TryRemove(firstTransactionId, out _);
            }
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
}