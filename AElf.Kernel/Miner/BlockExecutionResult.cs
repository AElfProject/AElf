using System;
using AElf.Kernel.BlockValidationFilters;

namespace AElf.Kernel.Miner
{
    public class BlockExecutionResult
    {
        public ValidationError? ValidationError { get; private set; }
        public bool Executed { get; private set; } //todo make this more explicit (ExecutionError)

        public Exception ExecutionException { get; set; }

        public BlockExecutionResult(bool executed = true, ValidationError? validationError = null)
        {
                
        }

        public BlockExecutionResult(Exception e) : this()
        {
            ExecutionException = e;
        }
    }
}