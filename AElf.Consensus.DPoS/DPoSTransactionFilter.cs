using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSTransactionFilter : IConsensusTransactionFilter
    {
        public List<Transaction> RemoveTransactionsJustForBroadcasting(ref List<Transaction> transactions)
        {
            var forBroadcasting = new List<Transaction>();

            foreach (var transaction in transactions)
            {
                if (transaction.MethodName == "BroadcastInValue")
                {
                    forBroadcasting.Add(transaction);
                }
            }

            foreach (var transaction in forBroadcasting)
            {
                transactions.Remove(transaction);
            }

            return forBroadcasting;
        }
    }
}