using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionsReceivedEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
    }
}