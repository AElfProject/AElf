using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IGrouper
    {
        List<ITransactionParallelGroup> ProduceGroup(Dictionary<Hash, List<ITransaction>> txList);
    }
}