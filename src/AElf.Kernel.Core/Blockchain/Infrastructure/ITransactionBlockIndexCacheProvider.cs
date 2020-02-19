using AElf.Types;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    //TODO: what's the interface for?
    public interface ITransactionBlockIndexCacheProvider
    {
        void AddOrUpdate(Hash transactionId, TransactionBlockIndex transactionBlockIndex);
        bool TryGetValue(Hash transactionId, out TransactionBlockIndex transactionBlockIndex);
        void CleanByHeight(long blockHeight);
    }
}