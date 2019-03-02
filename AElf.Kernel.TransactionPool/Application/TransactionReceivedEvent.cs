using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionsReceivedEvent
    {
        public int ChainId { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
    }
}