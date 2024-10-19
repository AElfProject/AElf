using System;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.CSharp.CodeOps.Instructions;

public partial class StateWrittenInstructionInjector
{
    protected virtual FlowBehavior HandleExceptionWhileGettingStateSizeLimitInstruction(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}