using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IParallelGroupService
    {
        List<ITransactionParallelGroup> ProduceGroup(Dictionary<Hash, List<ITransaction>> txList);
    }
}