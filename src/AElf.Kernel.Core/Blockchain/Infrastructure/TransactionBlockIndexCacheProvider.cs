using System;
using System.Collections.Concurrent;
using System.Linq;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class TransactionBlockIndexCacheProvider : ITransactionBlockIndexCacheProvider,
        ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, TransactionBlockIndex> _transactionBlockIndices;
        private readonly ConcurrentDictionary<long, ConcurrentBag<Hash>> _transactionBlockHeightMapping;
        
        public ILogger<TransactionBlockIndexCacheProvider> Logger { get; set; }

        public TransactionBlockIndexCacheProvider()
        {
            _transactionBlockIndices = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
            _transactionBlockHeightMapping = new ConcurrentDictionary<long, ConcurrentBag<Hash>>();

            Logger = NullLogger<TransactionBlockIndexCacheProvider>.Instance;
        }

        public void AddOrUpdate(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            if (transactionBlockIndex == null)
                return;
            
            var minBlockHeight = Math.Max(transactionBlockIndex.BlockHeight,
                transactionBlockIndex.PreviousExecutionBlockIndexList.Any()
                    ? transactionBlockIndex.PreviousExecutionBlockIndexList.Max(t => t.BlockHeight)
                    : 0);

            _transactionBlockHeightMapping.AddOrUpdate(minBlockHeight, new ConcurrentBag<Hash> {transactionId},
                (key, value) =>
                {
                    value.Add(transactionId);
                    return value;
                });

            _transactionBlockIndices[transactionId] = transactionBlockIndex;
        }

        public bool TryGetValue(Hash transactionId, out TransactionBlockIndex transactionBlockIndex)
        {
            return _transactionBlockIndices.TryGetValue(transactionId, out transactionBlockIndex);
        }

        public void CleanByHeight(long blockHeight)
        {
            foreach (var mapping in _transactionBlockHeightMapping.Where(m => m.Key <= blockHeight).ToList())
            {
                foreach (var txId in mapping.Value)
                {
                    _transactionBlockIndices.TryRemove(txId, out _);
                }

                _transactionBlockHeightMapping.TryRemove(mapping.Key, out _);
            }
        }
    }
}