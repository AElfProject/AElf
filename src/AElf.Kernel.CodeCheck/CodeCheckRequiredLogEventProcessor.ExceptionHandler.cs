using System;
using AElf.ExceptionHandler;

namespace AElf.Kernel.CodeCheck;

public partial class CodeCheckRequiredLogEventProcessor
{
    protected virtual FlowBehavior HandleExceptionWhileRequiringCodeCheck(Exception ex)
    {
        Logger.LogError("Error while processing CodeCheckRequired log event. {0}", ex);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new CodeCheckJobException($"Error while processing CodeCheckRequired log event. {ex}")
        };
    }
}