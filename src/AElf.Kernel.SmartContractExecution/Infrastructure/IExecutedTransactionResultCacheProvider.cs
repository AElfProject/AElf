using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Types;
using Microsoft.Extensions.Caching.Memory;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public interface IExecutedTransactionResultCacheProvider
    {
        List<TransactionResult> GetTransactionResults(Hash blockHash);
        void AddTransactionResults(Hash blockHash, List<TransactionResult> transactionResults);
    }

    public class ExecutedTransactionResultCacheProvider : IExecutedTransactionResultCacheProvider, ISingletonDependency
    {
        private readonly MemoryCache _executedTransactionResultCache;

        private const int ExpirationTime = 4000;

        public ExecutedTransactionResultCacheProvider()
        {
            _executedTransactionResultCache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency =
                    TimeSpan.FromMilliseconds(ExpirationTime)
            });
        }

        public List<TransactionResult> GetTransactionResults(Hash blockHash)
        {
            if (_executedTransactionResultCache.TryGetValue(blockHash, out var result))
            {
                return result as List<TransactionResult>;
            }

            return null;
        }

        public void AddTransactionResults(Hash blockHash, List<TransactionResult> transactionResults)
        {
            _executedTransactionResultCache.Set(blockHash, transactionResults, TimeSpan.FromSeconds(ExpirationTime));
        }
    }
}