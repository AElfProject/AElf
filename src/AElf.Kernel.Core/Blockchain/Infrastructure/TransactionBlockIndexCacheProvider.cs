using System;
using System.Collections.Concurrent;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class TransactionBlockIndexCacheProvider : ITransactionBlockIndexCacheProvider,
        ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, TransactionBlockIndex> _transactionBlockIndices;
        private readonly ConcurrentDictionary<long, ConcurrentBag<Hash>> _transactionBlockHeightMapping;
        
        public TransactionBlockIndexCacheProvider()
        {
            _transactionBlockIndices = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
            _transactionBlockHeightMapping = new ConcurrentDictionary<long, ConcurrentBag<Hash>>();
        }

        public void AddOrUpdate(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            var minBlockHeight = Math.Min(transactionBlockIndex.BlockHeight,
                transactionBlockIndex.PreviousExecutionBlockIndexList.Min(t => t.BlockHeight));

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