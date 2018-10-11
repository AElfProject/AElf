using System;
using AElf.ChainController;

namespace AElf.Miner.Miner
{
    public class BlockExecutionResult
    {
        public BlockValidationResult BlockValidationResult { get; private set; }
        public bool Executed { get; private set; } //todo make this more explicit (ExecutionError)

        public Exception ExecutionException { get; set; }

        public BlockExecutionResult(bool executed, BlockValidationResult blockValidationResult)
        {
            Executed = executed;
            BlockValidationResult = blockValidationResult;
        }

        public BlockExecutionResult(Exception e)
        {
            ExecutionException = e;
        }
    }
}