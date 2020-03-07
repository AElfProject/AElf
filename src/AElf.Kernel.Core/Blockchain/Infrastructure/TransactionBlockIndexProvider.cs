using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class TransactionBlockIndexProvider : ITransactionBlockIndexProvider,
        ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, TransactionBlockIndex> _transactionBlockIndices;

        public ILogger<TransactionBlockIndexProvider> Logger { get; set; }

        public TransactionBlockIndexProvider()
        {
            _transactionBlockIndices = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
            Logger = NullLogger<TransactionBlockIndexProvider>.Instance;
        }

        public void AddTransactionBlockIndex(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            if (transactionBlockIndex == null)
                return;
            _transactionBlockIndices[transactionId] = transactionBlockIndex;
        }

        public bool TryGetTransactionBlockIndex(Hash transactionId, out TransactionBlockIndex transactionBlockIndex)
        {
            return _transactionBlockIndices.TryGetValue(transactionId, out transactionBlockIndex);
        }

        public void CleanByHeight(long blockHeight)
        {
            var res = new Dictionary<Hash, TransactionBlockIndex>();
            foreach (var txId in _transactionBlockIndices.Where(m => m.Value.BlockHeight <= blockHeight)
                .Select(mapping => mapping.Key).ToList())
            {
                if (_transactionBlockIndices.TryRemove(txId, out var transactionBlockIndex))
                    res.Add(txId, transactionBlockIndex);
            }

            Logger.LogInformation($"Transaction block index count {_transactionBlockIndices.Count} in provider.");
        }
    }
}