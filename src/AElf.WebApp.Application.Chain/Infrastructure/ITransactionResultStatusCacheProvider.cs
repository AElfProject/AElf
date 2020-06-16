using System;
using AElf.CSharp.Core;
using AElf.Types;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly MemoryCache _validateResultsCache;
        private readonly WebAppOptions _webAppOptions;

        public ILogger<TransactionResultStatusCacheProvider> Logger { get; set; }

        public TransactionResultStatusCacheProvider(IOptionsSnapshot<WebAppOptions> webAppOptions)
        {
            _webAppOptions = webAppOptions.Value;

            _validateResultsCache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency =
                    TimeSpan.FromMilliseconds(_webAppOptions.TransactionResultStatusCacheSeconds.Mul(1000).Div(10))
            });

            Logger = NullLogger<TransactionResultStatusCacheProvider>.Instance;
        }

        public void AddTransactionResultStatus(Hash transactionId)
        {
            _validateResultsCache.Set(transactionId, new TransactionValidateStatus(),
                TimeSpan.FromSeconds(_webAppOptions.TransactionResultStatusCacheSeconds));
        }

        public void ChangeTransactionResultStatus(Hash transactionId, TransactionValidateStatus status)
        {
            if (_validateResultsCache.TryGetValue(transactionId, out _))
            {
                //_validateResultsCache.Remove(transactionId);
                _validateResultsCache.Set(transactionId, status,
                    TimeSpan.FromSeconds(_webAppOptions.TransactionResultStatusCacheSeconds));
            }
        }

        public TransactionValidateStatus GetTransactionResultStatus(Hash transactionId)
        {
            _validateResultsCache.TryGetValue(transactionId, out var status);
            return status as TransactionValidateStatus;
        }
    }

    public class TransactionValidateStatus
    {
        public TransactionResultStatus TransactionResultStatus { get; set; }
        public string Error { get; set; }
    }
}