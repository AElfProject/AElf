namespace AElf.Kernel
{
    public interface ITransactionContext
    {
        Hash Origin { get; set; }
        Hash Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        ITransaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
    }
}
