namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionInclusivenessProvider : ITransactionInclusivenessProvider
    {
        public bool IsTransactionPackable { get; set; } = true;
    }
}