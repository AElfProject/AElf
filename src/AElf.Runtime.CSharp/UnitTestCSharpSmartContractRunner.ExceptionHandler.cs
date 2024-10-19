using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.Runtime.CSharp;

public partial class UnitTestCSharpSmartContractRunner
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileLoadingAssemblyByFullName(OperationCanceledException ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
}