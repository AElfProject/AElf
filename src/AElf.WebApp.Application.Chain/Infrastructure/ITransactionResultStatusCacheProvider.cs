using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.WebApp.Application.Chain.Infrastructure
{
    public interface ITransactionResultStatusCacheProvider
    {
        void SetTransactionResultStatus(Hash transactionId, TransactionValidateStatus status);
        TransactionValidateStatus GetTransactionResultStatus(Hash transactionId);
    }

    public class TransactionResultStatusCacheProvider : ITransactionResultStatusCacheProvider
    {
        private readonly WebAppOptions _webAppOptions;

        private readonly ConcurrentDictionary<Hash, TransactionValidateStatus> _transactionValidateResults =
            new ConcurrentDictionary<Hash, TransactionValidateStatus>();

        private readonly List<Hash> _transactionIds = new List<Hash>();

        public TransactionResultStatusCacheProvider(IOptionsSnapshot<WebAppOptions> webAppOptions)
        {
            _webAppOptions = webAppOptions.Value;
        }

        public void SetTransactionResultStatus(Hash transactionId, TransactionValidateStatus status)
        {
            // Only update status when current status is PendingValidation.
            _transactionValidateResults.AddOrUpdate(transactionId, status,
                (hash, oldStatus) => oldStatus.TransactionResultStatus == TransactionResultStatus.PendingValidation
                    ? status
                    : oldStatus);
            if (!_transactionIds.Contains(transactionId))
            {
                _transactionIds.Add(transactionId);
            }

            ClearOldTransactionResultStatus();
        }

        private void ClearOldTransactionResultStatus()
        {
            if (_transactionIds.Count < _webAppOptions.TransactionResultStatusCacheSize) return;

            var firstTransactionId = _transactionIds.First();
            if (_transactionValidateResults.TryRemove(firstTransactionId, out _))
            {
                _transactionIds.Remove(firstTransactionId);
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