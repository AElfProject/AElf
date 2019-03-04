using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionAcceptedEvent
    {
        public int ChainId { get; set; }
        public Transaction Transaction { get; set; }
    }
}