using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Events
{
    public class TxResourcesNeededEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
    }
    
    public class TxResourcesNoLongerNeededEvent
    {
        public IEnumerable<Hash> TxIds { get; set; }
    }
}
