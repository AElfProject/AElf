using System;
namespace AElf.Kernel
{
    public interface ITransactionContext
    {
        Hash ChainId { get; set; }
        Hash ContractAddress { get; set; }
        Hash PreviousBlockHash { get; set; }
        ITransaction Transaction { get; set; }
        TransactionResult TransactionResult { get; set; }
    }
}
