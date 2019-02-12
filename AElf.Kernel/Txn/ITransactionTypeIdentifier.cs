namespace AElf.Kernel.Txn
{
    public interface ITransactionTypeIdentifier
    {
        bool IsDposTransaction(Transaction transaction);

        bool IsCrossChainIndexingTransaction(Transaction transaction);

        bool IsSystemTransaction(Transaction transaction);

        bool IsClaimFeesTransaction(Transaction transaction);
    }
}