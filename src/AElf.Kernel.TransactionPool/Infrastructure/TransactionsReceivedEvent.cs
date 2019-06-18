using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionsReceivedEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
    }
}