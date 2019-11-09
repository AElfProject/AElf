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

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<Hash, TransactionBlockIndex>>
            _transactionBlockHeightMapping;
        
        public ILogger<TransactionBlockIndexCacheProvider> Logger { get; set; }

        public TransactionBlockIndexCacheProvider()
        {
            _transactionBlockIndices = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
            _transactionBlockHeightMapping =
                new ConcurrentDictionary<long, ConcurrentDictionary<Hash, TransactionBlockIndex>>();

            Logger = NullLogger<TransactionBlockIndexCacheProvider>.Instance;
        }

        public void AddOrUpdate(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            if (transactionBlockIndex == null)
                return;

            if (_transactionBlockIndices.TryGetValue(transactionId, out var existingTransactionBlockIndex))
            {
                var blockHeight = Math.Max(existingTransactionBlockIndex.BlockHeight,
                    existingTransactionBlockIndex.PreviousExecutionBlockIndexList.Any()
                        ? existingTransactionBlockIndex.PreviousExecutionBlockIndexList.Max(t => t.BlockHeight)
                        : 0);

                _transactionBlockHeightMapping[blockHeight].TryRemove(transactionId, out _);
            }

            var maxBlockHeight = Math.Max(transactionBlockIndex.BlockHeight,
                transactionBlockIndex.PreviousExecutionBlockIndexList.Any()
                    ? transactionBlockIndex.PreviousExecutionBlockIndexList.Max(t => t.BlockHeight)
                    : 0);

            if (!_transactionBlockHeightMapping.TryGetValue(maxBlockHeight, out var mapping))
            {
                mapping = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
                _transactionBlockHeightMapping.TryAdd(maxBlockHeight, mapping);
            }

            mapping.TryAdd(transactionId, transactionBlockIndex);

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
                foreach (var txId in mapping.Value.Keys)
                {
                    _transactionBlockIndices.TryRemove(txId, out _);
                }

                _transactionBlockHeightMapping.TryRemove(mapping.Key, out _);
            }
        }
    }
}