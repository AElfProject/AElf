namespace AElf.Kernel.Miner.Application
{
    public interface ITransactionTypeIdentificationService
    {
        bool IsSystemTransaction(Transaction transaction);
        bool CanBeBroadCast(Transaction transaction);
    }
}