using System;
namespace AElf.Kernel
{
    public interface ITransactionContext
    {
        Hash PreviousBlockHash { get; set; }
        ITransaction Transaction { get; set; }
        TransactionResult TransactionResult { get; set; }
    }
}
