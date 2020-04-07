using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutionResult
    {
        public List<BlockExecutedSet> SuccessBlockExecutedSets { get; set; }

        public List<Block> ExecutedFailedBlocks { get; set; }

        public BlockExecutionResult()
        {
            SuccessBlockExecutedSets = new List<BlockExecutedSet>();
            ExecutedFailedBlocks = new List<Block>();
        }
    }
}