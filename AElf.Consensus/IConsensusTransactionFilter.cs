using System.Collections;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusTransactionFilter
    {
        List<Transaction> RemoveTransactionsJustForBroadcasting(ref List<Transaction> transactions);
    }
}