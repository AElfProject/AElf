using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Miner.EventMessages
{
    public sealed class TransactionsExecuted
    {
        public TransactionsExecuted(IReadOnlyList<Transaction> transactions, ulong blockNumber)
        {
            Transactions = transactions;
            BlockNumber = blockNumber;
        }

        public IReadOnlyList<Transaction> Transactions { get; }
        public ulong BlockNumber { get; }
    }
}