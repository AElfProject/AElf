using System.Collections.Generic;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class UnexecutableTransactionsFoundEvent
    {
        public UnexecutableTransactionsFoundEvent(BlockHeader header, List<Hash> transactions)
        {
            BlockHeader = header;
            Transactions = transactions;
        }

        public BlockHeader BlockHeader { get; }
        public List<Hash> Transactions { get; }
    }
}