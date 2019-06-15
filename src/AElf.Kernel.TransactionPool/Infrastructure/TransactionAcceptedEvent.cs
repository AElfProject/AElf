using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionAcceptedEvent
    {
        public int ChainId { get; set; }
        public Transaction Transaction { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
}