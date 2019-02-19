namespace AElf.Kernel
{
    public interface ITransactionTypeIdentifier
    {
        bool IsSystemTransaction(int chainId, Transaction transaction);
        bool CanBeBroadCast(int chainId, Transaction transaction);
    }
}