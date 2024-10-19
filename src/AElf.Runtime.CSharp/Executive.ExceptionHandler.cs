using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel;

namespace AElf.Runtime.CSharp;

public partial class Executive
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileExecuting(Exception ex)
    {
        CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
        CurrentTransactionContext.Trace.Error += ex + "\n";
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionAfterExecuting()
    {
        Cleanup();
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileExecutingTransaction(Exception ex)
    {
        CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
        CurrentTransactionContext.Trace.Error += ex + "\n";
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}