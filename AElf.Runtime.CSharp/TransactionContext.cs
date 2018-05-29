using System;
using AElf.Kernel;

namespace AElf.Runtime.CSharp
{
    public class TransactionContext : ITransactionContext
    {
        public Hash ChainId { get; set; }
        public Hash ContractAddress { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ITransaction Transaction { get; set; }
        public TransactionResult TransactionResult { get; set; }
    }
}
