using System.Collections.Generic;

namespace AElf.Kernel.SmartContractExecution.Events
{
    public class BlocksExecutionSucceededEvent
    {
        public List<Block> ExecutedBlocks { get; set; }
    }
}