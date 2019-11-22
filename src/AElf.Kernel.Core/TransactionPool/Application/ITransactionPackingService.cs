namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionPackingService
    {
        void EnableTransactionPacking();
        void DisableTransactionPacking();
        bool IsTransactionPackingEnabled();
    }
}