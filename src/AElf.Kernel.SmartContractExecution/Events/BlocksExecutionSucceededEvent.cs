using System.Collections.Generic;
using AElf.Kernel.Blockchain;

namespace AElf.Kernel.SmartContractExecution.Events
{
    public class BlocksExecutionSucceededEvent
    {
        public List<BlockExecutedSet> BlockExecutedSets { get; set; }
    }
}