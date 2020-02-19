using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    //TODO: remove
    public interface ITransactionTypeIdentificationService
    {
        bool IsSystemTransaction(Transaction transaction);
        bool CanBeBroadCast(Transaction transaction);
    }
}