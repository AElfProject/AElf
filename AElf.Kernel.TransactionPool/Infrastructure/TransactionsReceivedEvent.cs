using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionsReceivedEvent
    {
        public int ChainId { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
    }
}