using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Events
{
    public class TransactionResourcesNeededEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
    
    public class TransactionResourcesNoLongerNeededEvent
    {
        public IEnumerable<Hash> TransactionIds { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
}
