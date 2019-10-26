namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionInclusivenessProvider
    {
        bool IsTransactionPackable { get; set; }
    }
}