using System.Collections.Concurrent;
using System.Linq;

namespace AElf.Kernel.Blockchain.Infrastructure;

public class TransactionBlockIndexProvider : ITransactionBlockIndexProvider,
    ISingletonDependency
{
    private readonly ConcurrentDictionary<Hash, TransactionBlockIndex> _transactionBlockIndices;

    public TransactionBlockIndexProvider()
    {
        _transactionBlockIndices = new ConcurrentDictionary<Hash, TransactionBlockIndex>();
        Logger = NullLogger<TransactionBlockIndexProvider>.Instance;
    }

    public ILogger<TransactionBlockIndexProvider> Logger { get; set; }

    public void AddTransactionBlockIndex(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
    {
    }

    public bool TryGetTransactionBlockIndex(Hash transactionId, out TransactionBlockIndex transactionBlockIndex)
    {
        return _transactionBlockIndices.TryGetValue(transactionId, out transactionBlockIndex);
    }

    public void CleanByHeight(long blockHeight)
    {
        foreach (var txId in _transactionBlockIndices.Where(m => m.Value.BlockHeight <= blockHeight)
                     .Select(mapping => mapping.Key).ToList())
            _transactionBlockIndices.TryRemove(txId, out _);

        Logger.LogDebug("Transaction block index count {TransactionBlockIndicesCount} in provider",
            _transactionBlockIndices.Count);
    }
}