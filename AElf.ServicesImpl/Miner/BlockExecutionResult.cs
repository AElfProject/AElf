using System;

namespace AElf.Services.Miner
{
    public class BlockExecutionResult
    {
        public ValidationError ValidationError { get; private set; }
        public bool Executed { get; private set; } //todo make this more explicit (ExecutionError)

        public Exception ExecutionException { get; set; }

        public BlockExecutionResult(bool executed, ValidationError validationError)
        {
            Executed = executed;
            ValidationError = validationError;
        }

        public BlockExecutionResult(Exception e)
        {
            ExecutionException = e;
        }
    }
}