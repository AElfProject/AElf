using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Miner.EventMessages
{
    public sealed class TransactionExecuted
    {
        public TransactionExecuted(IReadOnlyList<Transaction> transactions)
        {
            Transactions = transactions;
        }

        public IReadOnlyList<Transaction> Transactions { get; }
    }
}