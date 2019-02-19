namespace AElf.Kernel.Miner.Application
{
    public interface ITransactionTypeIdentificationService
    {
        bool IsSystemTransaction(int chainId, Transaction transaction);
        bool CanBeBroadCast(int chainId, Transaction transaction);
    }
}