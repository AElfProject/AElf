using System.Collections.Concurrent;
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
            foreach (var txId in _transactionBlockIndices.Where(m => m.Value.BlockHeight <= blockHeight)
                .Select(mapping => mapping.Key).ToList())
            {
                _transactionBlockIndices.TryRemove(txId, out _);
            }

            Logger.LogDebug($"Transaction block index count {_transactionBlockIndices.Count} in provider.");
        }
    }
}