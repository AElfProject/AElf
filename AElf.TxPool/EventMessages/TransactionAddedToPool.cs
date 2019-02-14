using AElf.Kernel;

namespace AElf.TxPool.EventMessages
{
    public sealed class TransactionAddedToPool
    {
        public TransactionAddedToPool(Transaction transaction)
        {
            Transaction = transaction;
        }

        public Transaction Transaction { get; }
    }
}