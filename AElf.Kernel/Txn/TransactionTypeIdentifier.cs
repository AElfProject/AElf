namespace AElf.Kernel.Txn
{
    public abstract class TransactionTypeIdentifier : ITransactionTypeIdentifier
    {
        public virtual bool IsDposTransaction(Transaction transaction)
        {
            return false;
        }

        public virtual bool IsCrossChainIndexingTransaction(Transaction transaction)
        {
            return false;
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return IsDposTransaction(transaction) || IsCrossChainIndexingTransaction(transaction) ||
                   IsClaimFeesTransaction(transaction);
        }

        public virtual bool IsClaimFeesTransaction(Transaction transaction)
        {
            return false;
        }
    }
}