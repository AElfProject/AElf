using System;
namespace AElf.Kernel
{
    public class TransactionContext : ITransactionContext
    {
        public Hash PreviousBlockHash { get; set; }
        public ITransaction Transaction { get; set; }
        public TransactionResult TransactionResult { get; set; }
    }
}
