using System;
using AElf.ExceptionHandler;

namespace AElf.Kernel.CodeCheck.Application;

public partial class CodeCheckService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileAuditingContractCode(Exception ex)
    {
        Logger.LogWarning("Perform code check failed. {ExceptionMessage}", ex.Message);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}