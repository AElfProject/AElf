using System;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf;

public partial class TaskQueue
{
    protected virtual FlowBehavior HandleExceptionWhileExecutingFunction(Exception ex)
    {
        Logger.LogException(ex, LogLevel.Warning);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}