using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionsReceivedEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
    }
}