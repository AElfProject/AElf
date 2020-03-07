using AElf.Types;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface ITransactionBlockIndexProvider
    {
        void AddTransactionBlockIndex(Hash transactionId, TransactionBlockIndex transactionBlockIndex);
        bool TryGetTransactionBlockIndex(Hash transactionId, out TransactionBlockIndex transactionBlockIndex);
        void CleanByHeight(long blockHeight);
    }
}