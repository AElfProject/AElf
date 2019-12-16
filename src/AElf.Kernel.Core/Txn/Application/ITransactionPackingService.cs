namespace AElf.Kernel.Txn.Application
{
    public interface ITransactionPackingService
    {
        void EnableTransactionPacking();
        void DisableTransactionPacking();
        bool IsTransactionPackingEnabled();
    }
}