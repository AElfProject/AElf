using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public class ExecutableTransactionSet
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}