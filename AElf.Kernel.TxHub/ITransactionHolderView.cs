namespace AElf.Kernel
{
    public interface ITransactionHolderView
    {
        TxStatus Status { get; }
        Transaction Transaction { get; }
    }
}