namespace AElf.Kernel.Txn.Application
{
    public class TransactionPackingOptions
    {
        public bool IsTransactionPackable;

        public TransactionPackingOptions()
        {
            IsTransactionPackable = true;
        }
    }
}