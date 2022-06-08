using System.Collections.Generic;
using AElf.Kernel.Blockchain;

namespace AElf.Kernel.SmartContractExecution.Application;

public class BlockExecutionResult
{
    public BlockExecutionResult()
    {
        SuccessBlockExecutedSets = new List<BlockExecutedSet>();
        ExecutedFailedBlocks = new List<Block>();
    }

    public List<BlockExecutedSet> SuccessBlockExecutedSets { get; set; }

    public List<Block> ExecutedFailedBlocks { get; set; }
}