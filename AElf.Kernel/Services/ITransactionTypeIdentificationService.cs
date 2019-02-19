namespace AElf.Kernel.Services
{
    public interface ITransactionTypeIdentificationService
    {
        bool IsSystemTransaction(int chainId, Transaction transaction);
        bool CanBeBroadCast(int chainId, Transaction transaction);
    }
}