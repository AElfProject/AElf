using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IParallelGroupService
    {
        List<IParallelGroup> ProduceGroup(Dictionary<Hash, List<ITransaction>> txList);
    }
}