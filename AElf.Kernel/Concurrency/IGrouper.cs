using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IGrouper
    {
        List<TransactionParallelGroup> ProduceGroup(Dictionary<Hash, List<Transaction>> txList);
    }
}