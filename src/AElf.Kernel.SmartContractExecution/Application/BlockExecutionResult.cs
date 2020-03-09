using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutionResult
    {
        public List<Block> ExecutedSuccessBlocks { get; set; }

        public List<Block> ExecutedFailedBlocks { get; set; }

        public BlockExecutionResult()
        {
            ExecutedSuccessBlocks = new List<Block>();
            ExecutedFailedBlocks = new List<Block>();
        }
    }
}