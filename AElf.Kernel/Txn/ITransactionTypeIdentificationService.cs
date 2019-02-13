namespace AElf.Kernel.Txn
{
    public interface ITransactionTypeIdentificationService
    {
        bool IsSystemTransaction(Transaction transaction);
        bool CanBeBroadCast(Transaction transaction);
    }
}