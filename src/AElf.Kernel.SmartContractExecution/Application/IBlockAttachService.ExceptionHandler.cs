using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContractExecution.Application;

public partial class BlockAttachService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileExecutingBlocks(Exception ex)
    {
        Logger.LogError(ex, "Block execute fails.");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new BlockExecutionResult()
        };
    }
}